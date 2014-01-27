using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.CodeCompletion;
using JetBrains.ReSharper.Feature.Services.CSharp.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Feature.Services.Tips;
using JetBrains.ReSharper.PostfixTemplates.Settings;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Impl;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExpectedTypes;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve.Filters;
using JetBrains.ReSharper.Psi.Pointers;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Services;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.TextControl;
using JetBrains.Util;

// todo: caret placement for void methods? after ;? formatting?

namespace JetBrains.ReSharper.PostfixTemplates.CodeCompletion
{
  [Language(typeof(CSharpLanguage))]
  public class CSharpStaticMethodsItemProvider : CSharpItemsProviderBase<CSharpCodeCompletionContext>
  {
    protected override bool IsAvailable(CSharpCodeCompletionContext context)
    {
      var completionType = context.BasicContext.CodeCompletionType;
      return completionType == CodeCompletionType.AutomaticCompletion
          || completionType == CodeCompletionType.BasicCompletion;
    }

    protected override bool AddLookupItems(CSharpCodeCompletionContext context,
                                           GroupedItemsCollector collector)
    {
      var referenceExpression = CommonUtils.FindReferenceExpression(context.UnterminatedContext) ??
                                CommonUtils.FindReferenceExpression(context.TerminatedContext);
      if (referenceExpression == null) return false;

      var qualifier = referenceExpression.QualifierExpression;
      if (qualifier == null) return false;

      IType qualifierType = qualifier.Type(), filterType = qualifierType;
      if (!qualifierType.IsResolved)
      {
        qualifierType = IsEnumTypeReference(qualifier);
        if (qualifierType == null) return false;

        filterType = qualifier.GetPredefinedType().Type;
      }

      var settingsStore = qualifier.GetSettingsStore();
      if (!settingsStore.GetValue(PostfixSettingsAccessor.ShowStaticMethods))
        return false;

      // prepare symbol table of suitable static methods
      var accessContext = new ElementAccessContext(qualifier);
      var declaredTypes = DeclaredTypesCollector.Accept(qualifierType);

      // collect all declared types
      var psiModule = context.PsiModule;
      var commonSymbolTable = declaredTypes.Aggregate(
        seed: EmptySymbolTable.INSTANCE,
        func: (table, type) => table.Merge(type.GetSymbolTable(psiModule)));

      var filteredSymbolTable = commonSymbolTable.Filter(
        new SuitableStaticMethodsFilter(filterType, qualifier),
        OverriddenFilter.INSTANCE, new AccessRightsFilter(accessContext));

      var innerCollector = new GroupedItemsCollector();
      GetLookupItemsFromSymbolTable(filteredSymbolTable, innerCollector, context, false);

      // decorate static lookup elements
      foreach (var item in innerCollector.Items)
      {
        var lookupItem = item as DeclaredElementLookupItem;
        if (lookupItem == null) continue;

        lookupItem.TextColor = SystemColors.GrayText;
        SubscribeAfterComplete(lookupItem);
        collector.AddToBottom(lookupItem);
      }

      return true;
    }

    private static void SubscribeAfterComplete([NotNull] DeclaredElementLookupItem lookupItem)
    {
      // sorry, ugly as fuck :(
      lookupItem.AfterComplete += (ITextControl textControl, ref TextRange range,
                                   ref TextRange decoration, TailType tailType,
                                   ref Suffix suffix, ref IRangeMarker marker) =>
      {
        var solution = lookupItem.Solution;
        var psiServices = solution.GetPsiServices();
        psiServices.CommitAllDocuments();

        var methods = GetAllMethods(lookupItem);
        if (methods.Count == 0) return;

        var ownerType = methods[0].GetContainingType().NotNull();

        var hasMultipleParams = HasMultipleParameters(methods);
        if (!hasMultipleParams) // put caret 'foo(arg){here};'
        {
          var documentRange = new DocumentRange(textControl.Document, decoration);
          marker = documentRange.EndOffsetRange().CreateRangeMarker();
          // todo: review formatting here + check void return type?
        }

        var referenceExpression = TextControlToPsi
          .GetElement<IReferenceExpression>(solution, textControl.Document, range.StartOffset);
        if (referenceExpression == null) return;

        // 'remember' qualifier textually
        var qualifierExpression = referenceExpression.QualifierExpression.NotNull();
        var qualifierText = qualifierExpression.GetText();
        var referencePointer = referenceExpression.CreateTreeElementPointer();

        if (IsEnumTypeReference(qualifierExpression) != null)
        {
          qualifierText = "typeof(" + qualifierText + ")";
        }

        if (FirstArgumentAlwaysByRef(methods)) qualifierText = "ref " + qualifierText;
        if (HasOnlyMultipleParameters(methods)) qualifierText += ", ";

        var parenthesisRange = decoration.SetStartTo(range.EndOffset);
        var parenthesisMarker = parenthesisRange.CreateRangeMarker(textControl.Document);

        // insert qualifier as first argument
        var argPosition = TextRange.FromLength(decoration.EndOffset - (parenthesisRange.Length/2), 0);
        textControl.Document.ReplaceText(argPosition, qualifierText);

        // replace qualifier with type (predefined/user type)
        var keyword = CSharpTypeFactory.GetTypeKeyword(ownerType.GetClrName());
        var qualifierRange = qualifierExpression.GetDocumentRange().TextRange;
        textControl.Document.ReplaceText(qualifierRange, keyword ?? "T");

        psiServices.CommitAllDocuments();

        var newReference = referencePointer.GetTreeNode();
        if (newReference != null)
        {
          if (keyword == null) // bind user type
          {
            var qualifier = (IReferenceExpression) newReference.QualifierExpression.NotNull();
            var instance = lookupItem.PreferredDeclaredElement.NotNull();
            qualifier.Reference.BindTo(ownerType, instance.Substitution);

            range = newReference.NameIdentifier.GetDocumentRange().TextRange;
            decoration = TextRange.InvalidRange;
          }

          // show parameter info when needed
          if (hasMultipleParams && parenthesisMarker.IsValid)
          {
            var factory = solution.GetComponent<LookupItemsOwnerFactory>();
            var lookupItemsOwner = factory.CreateLookupItemsOwner(textControl);

            LookupUtil.ShowParameterInfo(
              solution, textControl, parenthesisMarker.Range, null, lookupItemsOwner);
          }
        }

        TipsManager.Instance.FeatureIsUsed(
          "Plugin.ControlFlow.PostfixTemplates.<static>", textControl.Document, solution);
      };
    }

    [CanBeNull]
    private static IDeclaredType IsEnumTypeReference([NotNull] ICSharpExpression expression)
    {
      var referenceExpression = expression as IReferenceExpression;
      if (referenceExpression == null) return null;

      var resolveResult = referenceExpression.Reference.Resolve().Result;

      var enumType = resolveResult.DeclaredElement as IEnum;
      if (enumType == null) return null;

      return TypeFactory.CreateType(enumType, resolveResult.Substitution);
    }

    [NotNull]
    private static IList<IMethod> GetAllMethods([NotNull] IDeclaredElementLookupItem lookupItem)
    {
      var results = new LocalList<IMethod>();

      var methodsItem = lookupItem as MethodsLookupItem;
      if (methodsItem != null)
      {
        foreach (var instance in methodsItem.Methods)
          results.Add(instance.Element);
      }
      else
      {
        var instance = lookupItem.PreferredDeclaredElement;
        if (instance != null)
        {
          var method = instance.Element as IMethod;
          if (method != null)
            results.Add(method);
        }
      }

      return results.ResultingList();
    }

    private sealed class DeclaredTypesCollector : TypeVisitor
    {
      private DeclaredTypesCollector() { }
      [NotNull] readonly HashSet<IDeclaredType> myTypes = new HashSet<IDeclaredType>();

      [NotNull] public static IEnumerable<IDeclaredType> Accept([NotNull] IType type)
      {
        var collector = new DeclaredTypesCollector();
        type.Accept(collector);

        return collector.myTypes;
      }

      public override void VisitDeclaredType(IDeclaredType declaredType)
      {
        if (!myTypes.Add(declaredType)) return;

        var typeElement = declaredType.GetTypeElement();
        if (typeElement == null) return;

        var substitution = declaredType.GetSubstitution();
        var typeParameters = typeElement.TypeParameters;
        if (typeParameters.Count == 0) return;

        foreach (var typeParameter in typeParameters)
        {
          substitution[typeParameter].Accept(this);
        }
      }

      public override void VisitArrayType(IArrayType arrayType)
      {
        var elementType = arrayType.ElementType;
        elementType.Accept(this);

        var resolveContext = elementType.GetResolveContext();
        if (resolveContext == null) return;

        var predefined = elementType.Module.GetPredefinedType(resolveContext);
        if (predefined.Array.IsResolved) predefined.Array.Accept(this);
      }

      public override void VisitType(IType type) {}
      public override void VisitMultitype(IMultitype multitype) {}
      public override void VisitDynamicType(IDynamicType dynamicType) {}
      public override void VisitPointerType(IPointerType pointerType) { }
      public override void VisitAnonymousType(IAnonymousType anonymousType) {}
    }

    private static bool HasMultipleParameters([NotNull] IList<IMethod> methods)
    {
      foreach (var method in methods)
      {
        var parameters = method.Parameters;
        if (parameters.Count > 1) return true;
        if (parameters.Count == 1 && parameters[0].IsParameterArray) return true;
      }

      return false;
    }

    private static bool HasOnlyMultipleParameters([NotNull] IList<IMethod> methods)
    {
      foreach (var method in methods)
      {
        if (method.Parameters.Count <= 1) return false;
      }

      return true;
    }

    private static bool FirstArgumentAlwaysByRef([NotNull] IList<IMethod> methods)
    {
      foreach (var method in methods)
      {
        var parameters = method.Parameters;
        if (parameters.Count > 0 && parameters[0].Kind == ParameterKind.REFERENCE) continue;

        return false;
      }

      return true;
    }

    private sealed class SuitableStaticMethodsFilter : SimpleSymbolFilter
    {
      [NotNull] private readonly IExpressionType myExpressionType;
      [NotNull] private readonly ICSharpTypeConversionRule myConversionRule;
      private readonly bool myAllowRefParameters;

      public SuitableStaticMethodsFilter([NotNull] IExpressionType expressionType,
                                         [NotNull] ICSharpExpression expression)
      {
        myExpressionType = expressionType;
        myConversionRule = expression.GetTypeConversionRule();

        var reference = expression as IReferenceExpression;
        if (reference != null && reference.QualifierExpression == null)
        {
          var element = reference.Reference.Resolve().DeclaredElement;
          myAllowRefParameters = (element is ILocalVariable || element is IParameter);
        }
      }

      public override ResolveErrorType ErrorType
      {
        get { return ResolveErrorType.NOT_RESOLVED; }
      }

      public override bool Accepts(IDeclaredElement declaredElement, ISubstitution substitution)
      {
        var method = declaredElement as IMethod;
        if (method == null || !method.IsStatic) return false;

        if (method.IsExtensionMethod) return false;
        if (method.Parameters.Count == 0) return false;

        switch (method.ShortName) // filter out static methods from Object.*
        {
          case "Equals":
          case "ReferenceEquals":
          {
            var containingType = method.GetContainingType();
            if (containingType.IsObjectClass()) return false;
            break;
          }
        }

        var firstParameter = method.Parameters[0];
        if (firstParameter.Kind != ParameterKind.VALUE)
        {
          if (!myAllowRefParameters) return false;
          if (firstParameter.Kind != ParameterKind.REFERENCE) return false;
        }

        var parameterType = firstParameter.Type;
        if (firstParameter.IsParameterArray)
        {
          var arrayType = parameterType as IArrayType;
          if (arrayType != null)
          {
            var elementType = arrayType.ElementType;
            if (IsConvertibleTo(myExpressionType, elementType))
              return true;
          }
        }

        return IsConvertibleTo(myExpressionType, parameterType);
      }

      private bool IsConvertibleTo([NotNull] IExpressionType expressionType, [NotNull] IType parameterType)
      {
        if (expressionType.IsImplicitlyConvertibleTo(parameterType, myConversionRule))
          return true;

        var declaredType = parameterType as IDeclaredType;
        if (declaredType != null)
        {
          var typeParameter = declaredType.GetTypeElement() as ITypeParameter;
          if (typeParameter != null)
          {
            var effectiveType = typeParameter.EffectiveBaseClass();
            if (effectiveType != null && expressionType.IsImplicitlyConvertibleTo(effectiveType, myConversionRule))
              return true;
          }
        }

        var parameterArrayType = parameterType as IArrayType;
        if (parameterArrayType != null)
        {
          var expressionArrayType = expressionType as IArrayType;
          if (expressionArrayType != null && expressionArrayType.Rank == parameterArrayType.Rank)
          {
            if (IsConvertibleTo(expressionArrayType.ElementType, parameterArrayType.ElementType))
              return true;
          }
        }

        return false;
      }
    }
  }
}
