using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
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
using JetBrains.ReSharper.Psi.Impl;
using JetBrains.ReSharper.Psi.Pointers;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.TextControl;
using JetBrains.Util;
using JetBrains.ReSharper.Feature.Services.Util;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems.Impl;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Info;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.BaseInfrastructure;
using JetBrains.ReSharper.Features.Intellisense.CodeCompletion.CSharp.Rules;
using ILookupItem = JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems.ILookupItem;

// todo: caret placement after completing generic method<>
// todo: gray color sometimes missing in 9.0 for bold items
// todo: AdjustTextColor

namespace JetBrains.ReSharper.PostfixTemplates.CodeCompletion
{
  [Language(typeof(CSharpLanguage))]
  public class CSharpStaticMethodsItemProvider : CSharpItemsProviderBase<CSharpCodeCompletionContext>
  {
    protected override bool IsAvailable(CSharpCodeCompletionContext context)
    {
      return context.BasicContext.IsAutoOrBasicCompletionType();
    }

    protected override bool AddLookupItems(CSharpCodeCompletionContext context, GroupedItemsCollector collector)
    {
      var referenceExpression = context.UnterminatedContext.ToReferenceExpression() ??
                                context.TerminatedContext.ToReferenceExpression();
      if (referenceExpression == null) return false;

      var qualifier = referenceExpression.QualifierExpression;
      if (qualifier == null) return false;

      var settingsStore = qualifier.GetSettingsStore();
      if (!settingsStore.GetValue(PostfixSettingsAccessor.ShowStaticMethods)) return false;

      IType filterType;
      var qualifierType = GetQualifierType(qualifier, out filterType);
      if (qualifierType == null) return false;

      var symbolTable = EmptySymbolTable.INSTANCE;
      foreach (var type in DeclaredTypesCollector.Accept(qualifierType))
      {
        symbolTable = symbolTable.Merge(type.GetSymbolTable(context.PsiModule));
      }

      var argumentsCount = GetExistingArgumentsCount(referenceExpression);

      // prepare symbol table of suitable static methods
      var accessFilter = new AccessRightsFilter(new ElementAccessContext(qualifier));
      var staticMethodsFilter = new SuitableStaticMethodsFilter(filterType, qualifier, argumentsCount);
      var filteredSymbolTable = symbolTable.Filter(staticMethodsFilter, OverriddenFilter.INSTANCE, accessFilter);

      FillCollectorWithStaticItems(context, collector, filteredSymbolTable, argumentsCount);
      return true;
    }

    private void FillCollectorWithStaticItems([NotNull] CSharpCodeCompletionContext context,
                                              [NotNull] GroupedItemsCollector collector,
                                              [NotNull] ISymbolTable staticsSymbolTable, int argumentsCount)
    {
      var innerCollector = context.BasicContext.CreateCollector();
      GetLookupItemsFromSymbolTable(staticsSymbolTable, innerCollector, context, false);

      var solution = context.BasicContext.Solution;

      // decorate static lookup elements
      foreach (var item in innerCollector.Items)
      {
#if RESHARPER91
        var lookupItem = item as IAspectLookupItem<DeclaredElementInfo>;
#else
        var lookupItem = item as ILookupItemWrapper<DeclaredElementInfo>;
#endif
        if (lookupItem == null) continue;
        
        var afterComplete = BakeAfterComplete(lookupItem, solution, argumentsCount);
        lookupItem.SubscribeAfterComplete(afterComplete);

#if RESHARPER91
        collector.Add(lookupItem);
#else
        collector.AddToBottom(lookupItem);
#endif
      }
    }

    private static int GetExistingArgumentsCount([NotNull] IReferenceExpression referenceExpression)
    {
      var invocationExpression = InvocationExpressionNavigator.GetByInvokedExpression(referenceExpression);
      if (invocationExpression == null) return 0;

      return invocationExpression.Arguments.Count;

      //var arguments = invocationExpression.Arguments;
      //
      //// TODO: may be reference from next line parsed as argument
      //
      //// special case: "Foo(" parses as invocation with 1 argument
      //if (arguments.Count == 1)
      //{
      //  var textRange = arguments[0].GetTreeTextRange();
      //  if (textRange.Length == 0) return 0;
      //}
      //
      //return arguments.Count;
    }

    [NotNull]
    private static AfterCompletionHandler BakeAfterComplete([NotNull] ILookupItem lookupItem, [NotNull] ISolution solution, int argumentsCount)
    {
      // sorry, ugly as fuck :(
      return (ITextControl textControl, ref TextRange range, ref TextRange decoration,
              TailType tailType, ref Suffix suffix, ref IRangeMarker caretMarker) =>
      {
        var psiServices = solution.GetPsiServices();
        psiServices.CommitAllDocuments();

        var allMethods = GetAllTargetMethods(lookupItem);
        if (allMethods.Count == 0) return;

        var reference = TextControlToPsi.GetElement<IReferenceExpression>(solution, textControl.Document, range.StartOffset);
        if (reference == null) return;

        var decorationText = textControl.Document.GetText(decoration);
        var decorationRange = new DocumentRange(textControl.Document, decoration);

        var hasMoreParameters = HasMoreParameters(allMethods, argumentsCount);
        if (!hasMoreParameters) // put caret 'foo(arg){here};'
        {
          caretMarker = decorationRange.EndOffsetRange().CreateRangeMarker();
        }
        else if (argumentsCount > 0)
        {
          var parenthesisCloseIndex = decorationText.LastIndexOf(')');
          if (parenthesisCloseIndex >= 0)
          {
            var delta = decoration.Length - parenthesisCloseIndex;
            caretMarker = decorationRange.EndOffsetRange().Shift(-delta).CreateRangeMarker();
          }
        }

        var qualifierExpression = reference.QualifierExpression.NotNull("qualifierExpression != null");
        var referencePointer = reference.CreateTreeElementPointer();

        var qualifierText = InsertQualifierAsArgument(
          qualifierExpression, allMethods, argumentsCount, textControl, decoration, decorationText);

        // TODO: mmm?
        if (!hasMoreParameters && !decorationText.EndsWith(")", StringComparison.Ordinal))
        {
          caretMarker = caretMarker.DocumentRange.Shift(+qualifierText.Length).CreateRangeMarker();
        }

        // replace qualifier with type (predefined/user type)
        var ownerType = allMethods[0].GetContainingType().NotNull("ownerType != null");
        FixQualifierExpression(textControl, qualifierExpression, ownerType);

        psiServices.CommitAllDocuments();

        var newReference = referencePointer.GetTreeNode();
        if (newReference != null)
        {
          var keyword = CSharpTypeFactory.GetTypeKeyword(ownerType.GetClrName());
          if (keyword == null) // bind user type
          {
            var newQualifier = (IReferenceExpression) newReference.QualifierExpression;
            if (newQualifier != null)
            {
              var elementInstance = lookupItem.GetDeclaredElement().NotNull("elementInstance != null");
              newQualifier.Reference.BindTo(ownerType, elementInstance.Substitution);
            }

            range = newReference.NameIdentifier.GetDocumentRange().TextRange;
            decoration = TextRange.InvalidRange;
          }

          // show parameter info when needed
          if (hasMoreParameters)
          {
            var factory = solution.GetComponent<LookupItemsOwnerFactory>();
            var lookupItemsOwner = factory.CreateLookupItemsOwner(textControl);

            LookupUtil.ShowParameterInfo(solution, textControl, lookupItemsOwner);
          }
        }

        TipsManager.Instance.FeatureIsUsed(
          "Plugin.ControlFlow.PostfixTemplates.<static>", textControl.Document, solution);
      };
    }

    [NotNull]
    private static string InsertQualifierAsArgument([NotNull] ICSharpExpression qualifierExpression,
                                                    [NotNull] IList<IMethod> allMethods, int argumentsCount,
                                                    [NotNull] ITextControl textControl, TextRange decoration,
                                                    [NotNull] string decorationText)
    {
      var qualifierText = qualifierExpression.GetText();

      var enumerationType = FindEnumerationType(qualifierExpression);
      if (enumerationType != null)
      {
        qualifierText = "typeof(" + qualifierText + ")";
      }

      if (FirstArgumentAlwaysPassByRef(allMethods))
      {
        qualifierText = "ref " + qualifierText;
      }

      if (argumentsCount > 0 || HasOnlyMultipleParameters(allMethods))
      {
        qualifierText += ", ";
      }

      var parenthesisOpenIndex = decorationText.IndexOf('(');
      if (parenthesisOpenIndex < 0)
      {
        qualifierText = "(" + qualifierText;
      }

      // insert qualifier as first argument
      var shift = (parenthesisOpenIndex >= 0) ? parenthesisOpenIndex + 1 : decoration.Length;
      var argPosition = TextRange.FromLength(decoration.StartOffset + shift, 0);

      textControl.Document.ReplaceText(argPosition, qualifierText);
      return qualifierText;
    }

    private static void FixQualifierExpression([NotNull] ITextControl textControl, [NotNull] ICSharpExpression expression, [NotNull] ITypeElement ownerType)
    {
      var qualifierRange = expression.GetDocumentRange().TextRange;

      var comparer = DeclaredElementEqualityComparer.TypeElementComparer;

      // do not produce type qualifier when static method from containing type completed
      var typeDeclaration = expression.GetContainingTypeDeclaration();
      if (typeDeclaration != null && comparer.Equals(typeDeclaration.DeclaredElement, ownerType))
      {
        var reference = ReferenceExpressionNavigator.GetByQualifierExpression(expression).NotNull("reference != null");

        var delimiter = reference.Delimiter;
        if (delimiter != null)
          qualifierRange = qualifierRange.JoinRight(delimiter.GetDocumentRange().TextRange);

        textControl.Document.ReplaceText(qualifierRange, string.Empty);
      }
      else
      {
        var keyword = CSharpTypeFactory.GetTypeKeyword(ownerType.GetClrName());
        textControl.Document.ReplaceText(qualifierRange, keyword ?? "T");
      }
    }

    [CanBeNull]
    private static IType GetQualifierType([NotNull] ICSharpExpression qualifier, [NotNull] out IType filterType)
    {
      var qualifierType = filterType = qualifier.Type();
      if (qualifierType.IsResolved) return qualifierType;

      var enumType = FindEnumerationType(qualifier);
      if (enumType == null) return null;

      filterType = qualifier.GetPredefinedType().Type;
      return enumType;
    }

    [CanBeNull]
    private static IDeclaredType FindEnumerationType([NotNull] ICSharpExpression expression)
    {
      var referenceExpression = expression as IReferenceExpression;
      if (referenceExpression == null) return null;

      var resolveResult = referenceExpression.Reference.Resolve().Result;

      var enumType = resolveResult.DeclaredElement as IEnum;
      if (enumType == null) return null;

      return TypeFactory.CreateType(enumType, resolveResult.Substitution);
    }

    [NotNull]
    private static IList<IMethod> GetAllTargetMethods([NotNull] ILookupItem lookupItem)
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

        var predefined = elementType.Module.GetPredefinedType(resolveContext);
        if (predefined.Array.IsResolved) predefined.Array.Accept(this);
      }

      public override void VisitType(IType type) {}
      public override void VisitMultitype(IMultitype multitype) {}
      public override void VisitDynamicType(IDynamicType dynamicType) {}
      public override void VisitPointerType(IPointerType pointerType) { }
      public override void VisitAnonymousType(IAnonymousType anonymousType) {}
    }

    private static bool HasMoreParameters([NotNull] IList<IMethod> methods, int argumentsCount)
    {
      foreach (var method in methods)
      {
        var parameters = method.Parameters;
        if (parameters.Count > 1 + argumentsCount) return true;
        if (parameters.Count > 0)
        {
          var lastIndex = parameters.Count - 1;
          if (parameters[lastIndex].IsParameterArray) return true;
        }
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

    private static bool FirstArgumentAlwaysPassByRef([NotNull] IList<IMethod> methods)
    {
      foreach (var method in methods)
      {
        var parameters = method.Parameters;
        if (parameters.Count == 0 || parameters[0].Kind != ParameterKind.REFERENCE) return false;
      }

      return true;
    }

    private sealed class SuitableStaticMethodsFilter : SimpleSymbolFilter
    {
      [NotNull] private readonly IExpressionType myExpressionType;
      [NotNull] private readonly ICSharpTypeConversionRule myConversionRule;
      private readonly int myExistingArgumentsCount;
      private readonly bool myAllowRefParameters;

      public SuitableStaticMethodsFilter(
        [NotNull] IExpressionType expressionType, [NotNull] ICSharpExpression expression, int argumentsCount)
      {
        myExpressionType = expressionType;
        myExistingArgumentsCount = argumentsCount;
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
        if (method.Parameters.Count <= myExistingArgumentsCount) return false;

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
        if (!firstParameter.IsParameterArray)
        {
          if (method.Parameters.Count <= myExistingArgumentsCount) return false;
        }

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
