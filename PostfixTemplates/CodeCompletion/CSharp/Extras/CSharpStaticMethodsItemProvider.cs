using System.Collections.Generic;
using System.Drawing;
using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.CodeCompletion;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.BaseInfrastructure;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Info;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.Feature.Services.CSharp.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Feature.Services.Util;
using JetBrains.ReSharper.Features.Intellisense.CodeCompletion.CSharp.AspectLookupItems;
using JetBrains.ReSharper.Features.Intellisense.CodeCompletion.CSharp.Rules;
using JetBrains.ReSharper.PostfixTemplates.Settings;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.CodeStyle.Suggest;
using JetBrains.ReSharper.Psi.CSharp.Impl;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve.Filters;
using JetBrains.ReSharper.Psi.Pointers;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.TextControl;
using JetBrains.Util;

// todo: caret placement after completing generic method<>
// todo: gray color sometimes missing in 9.0 for bold items
// todo: AdjustTextColor
// todo: disable for ?.

namespace JetBrains.ReSharper.PostfixTemplates.CodeCompletion.CSharp
{
  [Language(typeof(CSharpLanguage))]
  public class CSharpStaticMethodsItemProvider : CSharpItemsProviderBase<CSharpCodeCompletionContext>
  {
    protected override bool IsAvailable(CSharpCodeCompletionContext context)
    {
      return context.BasicContext.CodeCompletionType == CodeCompletionType.BasicCompletion;
    }

    protected override bool AddLookupItems(CSharpCodeCompletionContext context, GroupedItemsCollector collector)
    {
      var referenceExpression = context.UnterminatedContext.ToReferenceExpression() ??
                                context.TerminatedContext.ToReferenceExpression();
      if (referenceExpression == null) return false;

      var qualifierExpression = referenceExpression.QualifierExpression;
      if (qualifierExpression == null) return false;

      var settingsStore = qualifierExpression.GetSettingsStore();
      if (!settingsStore.GetValue(PostfixTemplatesSettingsAccessor.ShowStaticMethods)) return false;

      var qualifierType = qualifierExpression.Type();
      if (qualifierType.IsResolved) // 'a'.IsDigit -> char.IsDigit('a')
      {
        var symbolTable = GetStaticMethodsSymbolTable(referenceExpression, qualifierType, qualifierType);
        return AddStaticLookupItems(context, collector, symbolTable);
      }

      var enumerationType = FindReferencedEnumerationType(qualifierExpression);
      if (enumerationType != null) // E.GetNames -> Enum.GetNames(typeof(E))
      {
        var systemType = qualifierExpression.GetPredefinedType().Type;
        var symbolTable = GetStaticMethodsSymbolTable(referenceExpression, enumerationType, systemType);
        return AddStaticLookupItems(context, collector, symbolTable);
      }

      return false;
    }

    [NotNull] private static ISymbolTable GetStaticMethodsSymbolTable(
      [NotNull] IReferenceExpression referenceExpression, [NotNull] IType qualifierType, [NotNull] IType firstArgumentType)
    {
      var symbolTable = EmptySymbolTable.INSTANCE;
      var typesToTraverse = DeclaredTypesCollector.Accept(qualifierType);
      var psiModule = referenceExpression.GetPsiModule();

      foreach (var type in typesToTraverse)
      {
        symbolTable = symbolTable.Merge(type.GetSymbolTable(psiModule));
      }

      var argumentsCount = GetExistingArgumentsCount(referenceExpression);

      // prepare symbol table of suitable static methods
      var accessFilter = new AccessRightsFilter(referenceExpression.Reference.GetAccessContext());
      var qualifierExpression = referenceExpression.QualifierExpression.NotNull("qualifierExpression != null");

      var staticMethodsFilter = new StaticMethodsByFirstArgumentTypeFilter(firstArgumentType, qualifierExpression, argumentsCount);
      var filteredSymbolTable = symbolTable.Filter(staticMethodsFilter, OverriddenFilter.INSTANCE, accessFilter);

      return filteredSymbolTable;
    }

    [CanBeNull]
    private static IDeclaredType FindReferencedEnumerationType([NotNull] ICSharpExpression expression)
    {
      var referenceExpression = expression as IReferenceExpression;
      if (referenceExpression == null) return null;

      var resolveResult = referenceExpression.Reference.Resolve();

      var enumType = resolveResult.DeclaredElement as IEnum;
      if (enumType == null) return null;

      return TypeFactory.CreateType(enumType, resolveResult.Result.Substitution);
    }

    private static int GetExistingArgumentsCount([NotNull] IReferenceExpression referenceExpression)
    {
      var invocationExpression = InvocationExpressionNavigator.GetByInvokedExpression(referenceExpression);
      if (invocationExpression == null) return 0;

      return invocationExpression.Arguments.Count;
    }

    private bool AddStaticLookupItems(
      [NotNull] CSharpCodeCompletionContext context, [NotNull] GroupedItemsCollector collector, [NotNull] ISymbolTable staticsSymbolTable)
    {
      var innerCollector = context.BasicContext.CreateCollector();
      GetLookupItemsFromSymbolTable(staticsSymbolTable, innerCollector, context, includeFollowingExpression: false);

      foreach (var lookupItem in innerCollector.Items)
      {
        var declaredElementInfoItem = lookupItem as LookupItem<CSharpDeclaredElementInfo>;
        if (declaredElementInfoItem != null)
        {
          declaredElementInfoItem.WithBehavior(item => new StaticMethodBehavior(item.Info));
          declaredElementInfoItem.AdjustTextColor(Color.Gray);

          collector.Add(declaredElementInfoItem);
          continue;
        }

        var methodsInfoItem = lookupItem as LookupItem<MethodsInfo>;
        if (methodsInfoItem != null)
        {
          methodsInfoItem.WithBehavior(item => new StaticMethodBehavior(item.Info));
          methodsInfoItem.AdjustTextColor(Color.Gray);

          collector.Add(methodsInfoItem);
        }
      }

      return true;
    }

/*
    [NotNull]
    private static AfterCompletionHandler BakeAfterComplete([NotNull] ILookupItem lookupItem, [NotNull] ISolution solution, int argumentsCount)
    {
      // sorry, ugly as fuck :(
      return (ITextControl textControl, ref TextRange range, ref TextRange decoration,
              TailType tailType, ref Suffix suffix, ref IRangeMarker caretMarker) =>
      {
        var psiServices = solution.GetPsiServices();
        psiServices.Files.CommitAllDocuments();

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

        psiServices.Files.CommitAllDocuments();

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
*/

    private sealed class StaticMethodBehavior : LookupItemAspect<DeclaredElementInfo>, ILookupItemBehavior
    {
      [NotNull] private readonly List<DeclaredElementInstance> myMethods = new List<DeclaredElementInstance>();

      public StaticMethodBehavior([NotNull] CSharpDeclaredElementInfo info) : base(info)
      {
        var elementInstance = info.PreferredDeclaredElement;
        if (elementInstance != null) myMethods.Add(elementInstance);
      }

      public StaticMethodBehavior([NotNull] MethodsInfo info) : base(info)
      {
        var elementInstances = info.AllDeclaredElements;
        if (elementInstances != null) myMethods.AddRange(elementInstances);
      }

      public bool AcceptIfOnlyMatched(LookupItemAcceptanceContext itemAcceptanceContext) { return false; }

      public void Accept(
        ITextControl textControl, TextRange nameRange, LookupItemInsertType lookupItemInsertType,
        Suffix suffix, ISolution solution, bool keepCaretStill)
      {
        textControl.Document.ReplaceText(nameRange, Info.ShortName);

        var psiServices = solution.GetPsiServices();
        psiServices.Files.CommitAllDocuments();

        var referenceExpression = TextControlToPsi.GetElement<IReferenceExpression>(solution, textControl.Document, nameRange.StartOffset);
        if (referenceExpression == null) return;

        if (myMethods.Count == 0) return;

        var argumentsCount = GetExistingArgumentsCount(referenceExpression);
        var hasMoreParameters = HasMoreParametersToPass(argumentsCount);
        if (!hasMoreParameters)
        {
          // todo: put caret 'foo(arg){here}'
        }
        else if (argumentsCount > 0)
        {
          // todo: put caret 'foo(arg{here})'
        }

        //var qualifierExpression = referenceExpression.QualifierExpression.NotNull("qualifierExpression != null");
        var referencePointer = referenceExpression.CreateTreeElementPointer();



        InsertQualifierAsArgument(referenceExpression, argumentsCount, textControl);

        psiServices.Files.CommitAllDocuments();

        var treeNode = referencePointer.GetTreeNode();
        if (treeNode != null)
        {
          var newQualifier = treeNode.QualifierExpression as IReferenceExpression;
          if (newQualifier != null)
          {
            var containingType = ((IMethod) myMethods[0].Element).GetContainingType();
            // todo: without substitution? maybe with?

            psiServices.Transactions.Execute(
              commandName: typeof(StaticMethodBehavior).FullName,
              handler: () =>
              {
                newQualifier.Reference.BindTo(containingType.NotNull());

                CodeStyleUtil.ApplyStyle<StaticQualifierStyleSuggestion>(treeNode);

                var q = treeNode.QualifierExpression;
                if (q != null && q.IsValid())
                {
                  CodeStyleUtil.ApplyStyle<IBuiltInTypeReferenceStyleSuggestion>(treeNode);
                }
              });
          }
        }


        // replace qualifier with type (predefined/user type)
        //var ownerType = ((IMethod) myMethods[0].Element).GetContainingType().NotNull("ownerType != null");

        // todo: store pointer to reference expression

        //FixQualifierExpression(textControl, referenceExpression.QualifierExpression, ownerType);



        //var decorationText = textControl.Document.GetText(decoration);
        //var decorationRange = new DocumentRange(textControl.Document, decoration);

        //var hasMoreParameters = HasMoreParameters(allMethods, argumentsCount);
        //if (!hasMoreParameters) // put caret 'foo(arg){here};'
        //{
        //  caretMarker = decorationRange.EndOffsetRange().CreateRangeMarker();
        //}
        //else if (argumentsCount > 0)
        //{
        //  var parenthesisCloseIndex = decorationText.LastIndexOf(')');
        //  if (parenthesisCloseIndex >= 0)
        //  {
        //    var delta = decoration.Length - parenthesisCloseIndex;
        //    caretMarker = decorationRange.EndOffsetRange().Shift(-delta).CreateRangeMarker();
        //  }
        //}

        //var qualifierExpression = reference.QualifierExpression.NotNull("qualifierExpression != null");
        //var referencePointer = reference.CreateTreeElementPointer();
        //
        //var qualifierText = InsertQualifierAsArgument(
        //  qualifierExpression, allMethods, argumentsCount, textControl, decoration, decorationText);
        //
        //// TODO: mmm?
        //if (!hasMoreParameters && !decorationText.EndsWith(")", StringComparison.Ordinal))
        //{
        //  caretMarker = caretMarker.DocumentRange.Shift(+qualifierText.Length).CreateRangeMarker();
        //}
        //
        //// replace qualifier with type (predefined/user type)
        //var ownerType = allMethods[0].GetContainingType().NotNull("ownerType != null");
        //FixQualifierExpression(textControl, qualifierExpression, ownerType);
        //
        //psiServices.Files.CommitAllDocuments();
        //
        //var newReference = referencePointer.GetTreeNode();
        //if (newReference != null)
        //{
        //
        //  // show parameter info when needed
        //  if (hasMoreParameters)
        //  {
        //    var factory = solution.GetComponent<LookupItemsOwnerFactory>();
        //    var lookupItemsOwner = factory.CreateLookupItemsOwner(textControl);
        //
        //    LookupUtil.ShowParameterInfo(solution, textControl, lookupItemsOwner);
        //  }
        //}
      }

      [Pure] private bool HasMoreParametersToPass(int argumentsCount)
      {
        foreach (var methodInstance in myMethods)
        {
          var method = methodInstance.Element as IMethod;
          if (method == null) continue;

          var parameters = method.Parameters;
          if (parameters.Count > 1 + argumentsCount) return true;
          if (parameters.Count > 0)
          {
            var lastIndex = parameters.Count - 1;
            var lastParameter = parameters[lastIndex];
            if (lastParameter.IsParameterArray) return true;
          }
        }

        return false;
      }

      [Pure] private bool HasOnlyMultipleParameters()
      {
        foreach (var methodInstance in myMethods)
        {
          var method = methodInstance.Element as IMethod;
          if (method == null) continue;

          if (method.Parameters.Count <= 1) return false;
        }

        return true;
      }

      [Pure] private bool IsFirstArgumentAlwaysPassedByRef()
      {
        foreach (var methodInstance in myMethods)
        {
          var method = methodInstance.Element as IMethod;
          if (method == null) continue;

          var parameters = method.Parameters;
          if (parameters.Count == 0) return false;

          var firstParameter = parameters[0];
          if (firstParameter.Kind != ParameterKind.REFERENCE) return false;
        }

        return true;
      }

      private void InsertQualifierAsArgument(
        [NotNull] IReferenceExpression referenceExpression, int argumentsCount, [NotNull] ITextControl textControl)
      {
        var qualifierExpression = referenceExpression.QualifierExpression.NotNull("qualifierExpression != null");
        var qualifierText = qualifierExpression.GetText();

        var enumerationType = FindReferencedEnumerationType(qualifierExpression);
        if (enumerationType != null)
        {
          qualifierText = "typeof(" + qualifierText + ")";
        }

        if (IsFirstArgumentAlwaysPassedByRef())
        {
          qualifierText = "ref " + qualifierText;
        }

        if (argumentsCount > 0 || HasOnlyMultipleParameters())
        {
          qualifierText += ", ";
        }

        TextRange argPosition;

        var invokedExpression = InvocationExpressionNavigator.GetByInvokedExpression(referenceExpression);
        if (invokedExpression == null)
        {
          qualifierText = "(" + qualifierText;
          argPosition = referenceExpression.GetDocumentRange().EndOffsetRange().TextRange;

          // todo: check pars decoration setting
        }
        else
        {
          argPosition = invokedExpression.LPar.GetDocumentRange().EndOffsetRange().TextRange;
        }

        var qualifierDocumentRange = qualifierExpression.GetDocumentRange();

        textControl.Document.ReplaceText(argPosition, qualifierText);
        textControl.Document.ReplaceText(qualifierDocumentRange.TextRange, "T");


        // insert qualifier as first argument
        //var shift = (parenthesisOpenIndex >= 0) ? parenthesisOpenIndex + 1 : decoration.Length;
        //var argPosition = TextRange.FromLength(decoration.StartOffset + shift, 0);

        
        return;
      }
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

        var typeParameters = typeElement.TypeParameters;
        if (typeParameters.Count == 0) return;

        var substitution = declaredType.GetSubstitution();
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

        // add 'System.Array' static members for arrays
        var predefinedType = elementType.Module.GetPredefinedType(resolveContext);

        var systemArray = predefinedType.Array;
        if (systemArray.IsResolved) systemArray.Accept(this);
      }

      public override void VisitType(IType type) {}
      public override void VisitMultitype(IMultitype multitype) {}
      public override void VisitDynamicType(IDynamicType dynamicType) {}
      public override void VisitPointerType(IPointerType pointerType) { }
      public override void VisitAnonymousType(IAnonymousType anonymousType) {}
    }

    private sealed class StaticMethodsByFirstArgumentTypeFilter : SimpleSymbolFilter
    {
      [NotNull] private readonly IExpressionType myExpressionType;
      [NotNull] private readonly ICSharpTypeConversionRule myConversionRule;
      private readonly int myExistingArgumentsCount;
      private readonly bool myAllowRefParameters;

      public StaticMethodsByFirstArgumentTypeFilter([NotNull] IExpressionType expressionType, [NotNull] ICSharpExpression expression, int argumentsCount)
      {
        myExpressionType = expressionType;
        myExistingArgumentsCount = argumentsCount;
        myConversionRule = expression.GetTypeConversionRule();

        var reference = expression as IReferenceExpression;
        if (reference != null && reference.QualifierExpression == null)
        {
          var resolveResult = reference.Reference.Resolve();
          var declaredElement = resolveResult.DeclaredElement;

          myAllowRefParameters = (declaredElement is ILocalVariable || declaredElement is IParameter);
        }
      }

      public override ResolveErrorType ErrorType { get { return ResolveErrorType.NOT_RESOLVED; } }

      public override bool Accepts(IDeclaredElement declaredElement, ISubstitution substitution)
      {
        var method = declaredElement as IMethod;
        if (method == null || !method.IsStatic) return false;

        if (method.IsExtensionMethod) return false;

        var parameters = method.Parameters;
        if (parameters.Count <= myExistingArgumentsCount) return false;

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

        var firstParameter = parameters[0];
        if (!firstParameter.IsParameterArray)
        {
          if (parameters.Count <= myExistingArgumentsCount) return false;
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
          if (arrayType != null && IsConvertibleTo(myExpressionType, arrayType.ElementType))
            return true;
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
          // todo: not sure what is this used for
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
