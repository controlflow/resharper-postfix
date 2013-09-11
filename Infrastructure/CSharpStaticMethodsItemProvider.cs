using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.Settings;
using JetBrains.ReSharper.Feature.Services.CodeCompletion;
using JetBrains.ReSharper.Feature.Services.CSharp.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Impl;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve.Filters;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Services;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.TextControl;
using JetBrains.Util;
#if RESHARPER8
using JetBrains.ReSharper.Psi.ExpectedTypes;
using JetBrains.ReSharper.Psi.Pointers;
#endif

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion
{
  // todo: decorate step - hide overriden signatures (do we need this?)

  [Language(typeof(CSharpLanguage))]
  public class CSharpStaticMethodsItemProvider : CSharpItemsProviderBase<CSharpCodeCompletionContext>
  {
    protected override bool IsAvailable(CSharpCodeCompletionContext context)
    {
      var completionType = context.BasicContext.CodeCompletionType;
      return completionType == CodeCompletionType.AutomaticCompletion
          || completionType == CodeCompletionType.BasicCompletion;
    }

    protected override bool AddLookupItems(
      CSharpCodeCompletionContext context, GroupedItemsCollector collector)
    {
      var unterminated = context.UnterminatedContext;
      if (unterminated == null) return false;

      var reference = unterminated.Reference as IReferenceExpressionReference;
      if (reference == null) return false;

      var referenceExpression = (IReferenceExpression) reference.GetTreeNode();
      var qualifier = referenceExpression.QualifierExpression;
      if (qualifier == null) return false;

      var qualifierType = qualifier.Type();
      if (!qualifierType.IsResolved) return false;

      var settingsStore = qualifier.GetSettingsStore();
      if (!settingsStore.GetValue(PostfixSettingsAccessor.ShowStaticMethodsInCodeCompletion))
        return false;

      // prepare symbol table of suitable static methods
      var rule = referenceExpression.GetTypeConversionRule();
      var accessContext = new ElementAccessContext(qualifier);

      var typesCollector = new DeclaredTypesCollector();
      qualifierType.Accept(typesCollector);

      // collect all declared types
      var allTypesTable = typesCollector.Types
        .Aggregate(EmptySymbolTable.INSTANCE, (table, type) =>
          table.Merge(type.GetSymbolTable(context.PsiModule)))
        .Distinct();

      var symbolTable = allTypesTable.Filter(
        new StaticMethodWithFirstParamConvertibleToFilter(qualifierType, rule),
        OverriddenFilter.INSTANCE, new AccessRightsFilter(accessContext));

      var innerCollector = new GroupedItemsCollector();
      GetLookupItemsFromSymbolTable(symbolTable, innerCollector, context
#if RESHARPER8
        , false
#endif
        );

      // decorate static lookup elements
      var itemsOwner = context.BasicContext.LookupItemsOwner;
      foreach (var lookupItem in innerCollector.Items)
      {
        var item = lookupItem as DeclaredElementLookupItem;
        if (item == null) continue;

        item.TextColor = SystemColors.GrayText;
        SubscribeAfterComplete(item, itemsOwner);
        collector.AddToBottom(item);
      }

      return true;
    }

    private static void SubscribeAfterComplete(
      [NotNull] DeclaredElementLookupItem lookupItem, [NotNull] ILookupItemsOwner itemsOwner)
    {
      lookupItem.AfterComplete += (
#if RESHARPER7
        ITextControl textControl, ref TextRange range, ref TextRange decoration) => 
#elif RESHARPER8
        ITextControl textControl, ref TextRange range, ref TextRange decoration,
        TailType tailType, ref Suffix suffix, ref IRangeMarker marker) =>
#endif
      {
        var solution = lookupItem.Solution;
        var psiServices = solution.GetPsiServices();
        psiServices.CommitAllDocuments();

        var preferredDeclaredElement = lookupItem.PreferredDeclaredElement;
        if (preferredDeclaredElement == null) return;

        var method = (IMethod) preferredDeclaredElement.Element;
        var ownerType = method.GetContainingType().NotNull();
        var hasMultipleParams = HasMultipleParameters(lookupItem, method);

#if RESHARPER8
        if (!hasMultipleParams) // put caret 'foo(arg){here};'
        {
          var documentRange = new DocumentRange(textControl.Document, decoration);
          marker = documentRange.EndOffsetRange().CreateRangeMarker();
        }
#endif

        foreach (var referenceExpression in TextControlToPsi.GetElements<
          IReferenceExpression>(solution, textControl.Document, range.StartOffset))
        {
          // 'remember' qualifier textually
          var qualifierExpression = referenceExpression.QualifierExpression;
          var qualifierText = qualifierExpression.NotNull().GetText();
          var referencePointer = referenceExpression.CreateTreeElementPointer();
          var parenthesisRange = decoration.SetStartTo(range.EndOffset);
          var parenthesisMarker = parenthesisRange.CreateRangeMarker(textControl.Document);

          // append ', ' if all overloads with >1 arguments
          if (HasOnlyMultipleParameters(lookupItem, method)) qualifierText += ", ";

          // insert qualifier as first argument
          var argumentPosition = TextRange.FromLength(
            decoration.EndOffset - (parenthesisRange.Length / 2), 0);
          textControl.Document.ReplaceText(argumentPosition, qualifierText);

          // replace qualifier with type (predefined/user type)
          var keyword = CSharpTypeFactory.GetTypeKeyword(ownerType.GetClrName());
          var qualifierRange = qualifierExpression.GetDocumentRange().TextRange;
          textControl.Document.ReplaceText(qualifierRange, keyword ?? "T");

          psiServices.CommitAllDocuments();

          var newReference = referencePointer.GetTreeNode();
          if (newReference == null) break;

          if (keyword == null) // bind user type
          {
            var qualifier = (IReferenceExpression) newReference.QualifierExpression.NotNull();
            qualifier.Reference.BindTo(ownerType, preferredDeclaredElement.Substitution);
          }

          // show parameter info when needed
          if (hasMultipleParams && parenthesisMarker.IsValid)
          {
            LookupUtil.ShowParameterInfo(
              solution, textControl, parenthesisMarker.Range, null, itemsOwner);
          }

          break;
        }
      };
    }

    private sealed class DeclaredTypesCollector : TypeVisitor
    {
      public DeclaredTypesCollector()
      {
        Types = new List<IDeclaredType>();
      }

      [NotNull] public List<IDeclaredType> Types { get; private set; }

      public override void VisitDeclaredType(IDeclaredType declaredType)
      {
        Types.Add(declaredType);

        var typeElement = declaredType.GetTypeElement();
        if (typeElement != null)
        {
          var substitution = declaredType.GetSubstitution();
          var typeParameters = typeElement.TypeParameters;
          if (typeParameters.Count > 0)
          {
            foreach (var typeParameter in typeParameters)
              substitution[typeParameter].Accept(this);
          }
        }
      }

      public override void VisitArrayType(IArrayType arrayType)
      {
        arrayType.ElementType.Accept(this);
      }

      public override void VisitPointerType(IPointerType pointerType)
      {
        pointerType.ElementType.Accept(this);
      }

      public override void VisitType(IType type) { }
      public override void VisitMultitype(IMultitype multitype) { }
      public override void VisitDynamicType(IDynamicType dynamicType) { }
      public override void VisitAnonymousType(IAnonymousType anonymousType) { }
    }

    private static bool HasMultipleParameters(
      [NotNull] IDeclaredElementLookupItem item, [NotNull] IMethod method)
    {
      var methodsItem = item as MethodsLookupItem;
      if (methodsItem == null) return HasMultipleParameters(method);

      foreach (var instance in methodsItem.Methods)
        if (HasMultipleParameters(instance.Element)) return true;

      return false;
    }

    private static bool HasMultipleParameters([NotNull] IParametersOwner method)
    {
      var parameters = method.Parameters;
      if (parameters.Count > 1) return true;

      return parameters.Count == 1 && parameters[0].IsParameterArray;
    }

    private static bool HasOnlyMultipleParameters(
      [NotNull] IDeclaredElementLookupItem item, [NotNull] IMethod method)
    {
      var methodsItem = item as MethodsLookupItem;
      if (methodsItem == null)
        return method.Parameters.Count > 1;

      foreach (var instance in methodsItem.Methods)
        if (instance.Element.Parameters.Count <= 1) return false;

      return true;
    }

    private sealed class StaticMethodWithFirstParamConvertibleToFilter : SimpleSymbolFilter
    {
      [NotNull] private readonly IExpressionType myExpressionType;
      [NotNull] private readonly ICSharpTypeConversionRule myConversionRule;

      public StaticMethodWithFirstParamConvertibleToFilter(
        [NotNull] IExpressionType expressionType, [NotNull] ICSharpTypeConversionRule conversionRule)
      {
        myExpressionType = expressionType;
        myConversionRule = conversionRule;
      }

      public override ResolveErrorType ErrorType { get { return ResolveErrorType.NOT_RESOLVED; } }
      public override bool Accepts(IDeclaredElement declaredElement, ISubstitution substitution)
      {
        var method = declaredElement as IMethod;
        if (method != null && method.IsStatic && !method.IsExtensionMethod && method.Parameters.Count > 0)
        {
          // filter out static methods from Object.*
          if (method.GetContainingType().IsObjectClass()) return false;

          var firstParameter = method.Parameters[0];
          var parameterType = firstParameter.Type;

          if (firstParameter.IsParameterArray)
          {
            var arrayType = parameterType as IArrayType;
            if (arrayType != null && myExpressionType
              .IsImplicitlyConvertibleTo(arrayType.ElementType, myConversionRule)) return true;
          }

          return myExpressionType.IsImplicitlyConvertibleTo(parameterType, myConversionRule);
        }

        return false;
      }
    }
  }
}