using System.Collections.Generic;
using System.Drawing;
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
#if RESHARPER8
using ILookupItem = JetBrains.ReSharper.Feature.Services.Lookup.ILookupItem;
#elif RESHARPER9
using JetBrains.ReSharper.Features.Intellisense.CodeCompletion.CSharp.Rules;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems.Impl;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.BaseInfrastructure;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Info;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Presentations;
using ILookupItem = JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems.ILookupItem;
#endif

// todo: caret placement for void methods? after ;? formatting?
// todo: caret placement after completing generic method<>
// todo: better support existing arguments, fix HasMultiple and etc

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
      var referenceExpression = context.UnterminatedContext.ToReferenceExpression() ??
                                context.TerminatedContext.ToReferenceExpression();
      if (referenceExpression == null) return false;

      var qualifier = referenceExpression.QualifierExpression;
      if (qualifier == null) return false;

      var settingsStore = qualifier.GetSettingsStore();
      if (!settingsStore.GetValue(PostfixSettingsAccessor.ShowStaticMethods)) return false;

      IType filterType;
      var qualifierType = QualifierType(qualifier, out filterType);
      if (qualifierType == null) return false;

      // collect all declared types
      var symbolTable = EmptySymbolTable.INSTANCE;
      foreach (var type in DeclaredTypesCollector.Accept(qualifierType))
      {
        symbolTable = symbolTable.Merge(type.GetSymbolTable(context.PsiModule));
      }

      // prepare symbol table of suitable static methods
      var accessFilter = new AccessRightsFilter(new ElementAccessContext(qualifier));
      var filteredSymbolTable = symbolTable.Filter(
        new SuitableStaticMethodsFilter(filterType, qualifier),
        OverriddenFilter.INSTANCE, accessFilter);

#if RESHARPER9
      var innerCollector = context.BasicContext.CreateCollector();
#else
      var innerCollector = new GroupedItemsCollector();
#endif
      GetLookupItemsFromSymbolTable(filteredSymbolTable, innerCollector, context, false);

      var solution = context.BasicContext.Solution;

      // decorate static lookup elements
      foreach (var item in innerCollector.Items)
      {
#if RESHARPER8

        var lookupItem = item as DeclaredElementLookupItem;
        if (lookupItem == null) continue;

        lookupItem.AfterComplete += SubscribeAfterComplete(lookupItem, solution);
        lookupItem.TextColor = SystemColors.GrayText;

#elif RESHARPER9

        var lookupItem = item as LookupItemWrapper<DeclaredElementInfo>;
        if (lookupItem == null) continue;

        var afterComplete = SubscribeAfterComplete(lookupItem, solution);
        lookupItem.SubscribeAfterComplete(afterComplete);

        var presentation = lookupItem.Item.Presentation as DeclaredElementPresentation<DeclaredElementInfo>;
        if (presentation != null) presentation.TextColor = SystemColors.GrayText;

#endif
        collector.AddToBottom(lookupItem);
      }

      return true;
    }

    private static AfterCompletionHandler SubscribeAfterComplete([NotNull] ILookupItem lookupItem,
                                                                 [NotNull] ISolution solution)
    {
      // sorry, ugly as fuck :(
      return (ITextControl textControl, ref TextRange range, ref TextRange decoration,
              TailType tailType, ref Suffix suffix, ref IRangeMarker marker) =>
      {
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
        var parenthesisIndex = textControl.Document.GetText(decoration).IndexOf('(');

        // insert qualifier as first argument
        var shift = (parenthesisIndex >= 0) ? parenthesisIndex : 0;
        var argPosition = TextRange.FromLength(decoration.StartOffset + shift + 1, 0);
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
            var instance = lookupItem.GetDeclaredElement().NotNull();
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
    private static IType QualifierType([NotNull] ICSharpExpression qualifier, [NotNull] out IType filterType)
    {
      var qualifierType = filterType = qualifier.Type();
      if (qualifierType.IsResolved) return qualifierType;

      var enumType = IsEnumTypeReference(qualifier);
      if (enumType == null) return null;

      filterType = qualifier.GetPredefinedType().Type;
      return enumType;
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
    private static IList<IMethod> GetAllMethods([NotNull] ILookupItem lookupItem)
    {
      var results = new LocalList<IMethod>();

      foreach (var instance in lookupItem.GetAllDeclaredElementInstances())
      {
        var method = instance.Element as IMethod;
        if (method != null) results.Add(method);
      }

      return results.ResultingList();
    }

    private sealed class DeclaredTypesCollector : TypeVisitor
    {
      private DeclaredTypesCollector() { }
      [NotNull] readonly HashSet<IDeclaredType> myTypes = new HashSet<IDeclaredType>();

      [NotNull] public static HashSet<IDeclaredType> Accept([NotNull] IType type)
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
        if (parameters.Count == 0 || parameters[0].Kind != ParameterKind.REFERENCE) return false;
      }

      return true;
    }

    // todo: pass existing parameters count
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
