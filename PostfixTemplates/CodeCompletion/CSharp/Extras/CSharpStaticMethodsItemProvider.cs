using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
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
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve.Filters;
using JetBrains.ReSharper.Psi.Pointers;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.TextControl;
using JetBrains.Util;

// todo: caret placement after completing generic method<>
// todo: disable for ?.
// todo: replay suffix
// todo: generic type parameters (check if inferable)
// todo: invoke parameter info
// todo: format!

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

      var existingArgumentsCount = GetExistingArgumentsCount(referenceExpression);

      // prepare symbol table of suitable static methods
      var accessFilter = new AccessRightsFilter(referenceExpression.Reference.GetAccessContext());
      var qualifierExpression = referenceExpression.QualifierExpression.NotNull("qualifierExpression != null");

      var staticMethodsFilter = new StaticMethodsByFirstArgumentTypeFilter(firstArgumentType, qualifierExpression, existingArgumentsCount);
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
      if (invocationExpression == null) return 0; // todo: -1 to indicate there is no invocation after?

      return invocationExpression.Arguments.Count;
    }

    private bool AddStaticLookupItems(
      [NotNull] CSharpCodeCompletionContext context, [NotNull] GroupedItemsCollector collector, [NotNull] ISymbolTable staticsSymbolTable)
    {
      var innerCollector = context.BasicContext.CreateCollector();
      GetLookupItemsFromSymbolTable(staticsSymbolTable, innerCollector, context, includeFollowingExpression: false);

      var replaceRange = context.ReplaceRangeWithJoinedArguments;
      var ranges = replaceRange.IsValid ? context.CompletionRanges.WithReplaceRange(replaceRange) : context.CompletionRanges;

      foreach (var lookupItem in innerCollector.Items)
      {
        var declaredElementInfoItem = lookupItem as LookupItem<CSharpDeclaredElementInfo>;
        if (declaredElementInfoItem != null)
        {
          declaredElementInfoItem.WithBehavior(item => new StaticMethodBehavior(item.Info));
          declaredElementInfoItem.WithInitializedRanges(ranges, context.BasicContext);
          declaredElementInfoItem.AdjustTextColor(Color.Gray);

          collector.Add(declaredElementInfoItem);
          continue;
        }

        var methodsInfoItem = lookupItem as LookupItem<MethodsInfo>;
        if (methodsInfoItem != null)
        {
          methodsInfoItem.WithBehavior(item => new StaticMethodBehavior(item.Info));
          methodsInfoItem.WithInitializedRanges(ranges, context.BasicContext);
          methodsInfoItem.AdjustTextColor(Color.Gray);

          collector.Add(methodsInfoItem);
        }
      }

      return true;
    }

    private sealed class StaticMethodBehavior : CSharpDeclaredElementBehavior<DeclaredElementInfo>
    {
      [NotNull] private readonly List<DeclaredElementInstance<IMethod>> myMethods = new List<DeclaredElementInstance<IMethod>>();

      public StaticMethodBehavior([NotNull] CSharpDeclaredElementInfo info) : base(info)
      {
        var instance = info.PreferredDeclaredElement;
        if (instance == null) return;

        var method = instance.Element as IMethod;
        if (method != null)
          myMethods.Add(new DeclaredElementInstance<IMethod>(method, instance.Substitution));
      }

      public StaticMethodBehavior([NotNull] MethodsInfo info) : base(info)
      {
        var instances = info.AllDeclaredElements;
        if (instances == null) return;

        foreach (var instance in instances)
        {
          var method = instance.Element as IMethod;
          if (method == null) continue;

          myMethods.Add(new DeclaredElementInstance<IMethod>(method, instance.Substitution));
        }
      }

      public override void Accept(
        ITextControl textControl, TextRange nameRange, LookupItemInsertType lookupItemInsertType, Suffix suffix, ISolution solution, bool keepCaretStill)
      {
        if (lookupItemInsertType == LookupItemInsertType.Insert)
        {
          if (Info.InsertText.IndexOf(')') == -1) Info.InsertText += ")";
        }
        else
        {
          if (Info.ReplaceText.IndexOf(')') == -1) Info.ReplaceText += ")";
        }

        base.Accept(textControl, nameRange, lookupItemInsertType, suffix, solution, keepCaretStill);

        var psiServices = solution.GetPsiServices();
        psiServices.Files.CommitAllDocuments();

        var referenceExpression = TextControlToPsi.GetElement<IReferenceExpression>(solution, textControl.Document, nameRange.StartOffset);
        if (referenceExpression == null || myMethods.Count == 0) return;

        var existingArgumentsCount = GetExistingArgumentsCount(referenceExpression);
        var referencePointer = referenceExpression.CreateTreeElementPointer();

        InsertQualifierAsArgument(referenceExpression, existingArgumentsCount, textControl);

        psiServices.Files.CommitAllDocuments();

        var reference1 = referencePointer.GetTreeNode();
        if (reference1 != null) BindQualifierTypeExpression(reference1);

        var reference2 = referencePointer.GetTreeNode();
        if (reference2 != null) PlaceCaretAfterCompletion(textControl, reference2, existingArgumentsCount, lookupItemInsertType);
      }

      private void BindQualifierTypeExpression([NotNull] IReferenceExpression referenceExpression)
      {
        var newQualifierReference = referenceExpression.QualifierExpression as IReferenceExpression;
        if (newQualifierReference == null) return; // 'T.' is not found for some reason :\

        var containingType = myMethods
          .Select(instance => instance.Element)
          .SelectNotNull(method => method.GetContainingType())
          .FirstOrDefault();

        if (containingType == null) return;

        var psiServices = referenceExpression.GetPsiServices();
        psiServices.Transactions.Execute(
          commandName: typeof (StaticMethodBehavior).FullName,
          handler: () =>
          {
            newQualifierReference.Reference.BindTo(containingType.NotNull());

            CodeStyleUtil.ApplyStyle<StaticQualifierStyleSuggestion>(referenceExpression);

            var qualifierExpression = referenceExpression.QualifierExpression;
            if (qualifierExpression != null && qualifierExpression.IsValid())
            {
              CodeStyleUtil.ApplyStyle<IBuiltInTypeReferenceStyleSuggestion>(qualifierExpression);
            }
          });
      }

      private void PlaceCaretAfterCompletion(
        [NotNull] ITextControl textControl, [NotNull] IReferenceExpression referenceExpression, int existingArgumentsCount, LookupItemInsertType insertType)
      {
        var referenceRange = referenceExpression.GetDocumentRange();
        textControl.Caret.MoveTo(referenceRange.TextRange.EndOffset, CaretVisualPlacement.DontScrollIfVisible);

        var invocationExpression = InvocationExpressionNavigator.GetByInvokedExpression(referenceExpression);
        if (invocationExpression == null) return;

        var invocationRange = invocationExpression.GetDocumentRange();
        textControl.Caret.MoveTo(invocationRange.TextRange.EndOffset, CaretVisualPlacement.DontScrollIfVisible);

        var settingsStore = referenceExpression.GetSettingsStore();
        var parenthesesInsertType = settingsStore.GetValue(CodeCompletionSettingsAccessor.ParenthesesInsertType);
        var hasMoreParametersToPass = HasMoreParametersToPass(existingArgumentsCount);

        switch (parenthesesInsertType)
        {
          case ParenthesesInsertType.Both:
          {
            if (hasMoreParametersToPass)
            {
              var rightPar = invocationExpression.RPar;
              if (rightPar != null)
              {
                var rightParRange = rightPar.GetDocumentRange().TextRange;
                textControl.Caret.MoveTo(rightParRange.StartOffset, CaretVisualPlacement.DontScrollIfVisible);
              }
            }

            break;
          }

          case ParenthesesInsertType.Left:
          case ParenthesesInsertType.None:
          {
            // if in insert mode - drop right par and set caret to it's start offest
            if (insertType == LookupItemInsertType.Insert)
            {
              var rightPar = invocationExpression.RPar;
              if (rightPar != null)
              {
                var rightParRange = rightPar.GetDocumentRange().TextRange;

                PsiServices.Transactions.Execute(
                  commandName: "AAA",
                  handler: () =>
                  {
                    using (WriteLockCookie.Create())
                    {
                      LowLevelModificationUtil.DeleteChild(rightPar);
                    }
                  });

                textControl.Caret.MoveTo(rightParRange.StartOffset, CaretVisualPlacement.DontScrollIfVisible);
              }
            }

            break;
          }
        }
      }

      [Pure] private bool HasMoreParametersToPass(int existingArgumentsCount)
      {
        foreach (var methodInstance in myMethods)
        {
          var parameters = methodInstance.Element.Parameters;
          if (parameters.Count > 1 + existingArgumentsCount) return true;

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
          var method = methodInstance.Element;
          if (method.Parameters.Count <= 1) return false;
        }

        return true;
      }

      [Pure] private bool IsFirstArgumentAlwaysPassedByRef()
      {
        foreach (var methodInstance in myMethods)
        {
          var parameters = methodInstance.Element.Parameters;
          if (parameters.Count == 0) return false;

          var firstParameter = parameters[0];
          if (firstParameter.Kind != ParameterKind.REFERENCE) return false;
        }

        return true;
      }

      private void InsertQualifierAsArgument([NotNull] IReferenceExpression referenceExpression, int existingArgumentsCount, [NotNull] ITextControl textControl)
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

        if (existingArgumentsCount > 0 || HasOnlyMultipleParameters())
        {
          qualifierText += ", ";
        }

        TextRange argPosition;

        // todo: not relible!
        var invokedExpression = InvocationExpressionNavigator.GetByInvokedExpression(referenceExpression);
        if (invokedExpression == null)
        {
          qualifierText = "(" + qualifierText;
          argPosition = referenceExpression.GetDocumentRange().EndOffsetRange().TextRange;

          // todo: check pars decoration setting
          // todo: insert ')' if it's insertion is disabled


        }
        else
        {
          argPosition = invokedExpression.LPar.GetDocumentRange().EndOffsetRange().TextRange;
        }

        var qualifierDocumentRange = qualifierExpression.GetDocumentRange();

        textControl.Document.ReplaceText(argPosition, qualifierText);
        textControl.Document.ReplaceText(qualifierDocumentRange.TextRange, "T");
      }
    }

    private sealed class DeclaredTypesCollector : TypeVisitor
    {
      private DeclaredTypesCollector()
      {
      }

      [NotNull] private readonly HashSet<IDeclaredType> myTypes = new HashSet<IDeclaredType>();

      [NotNull]
      public static HashSet<IDeclaredType> Accept([NotNull] IType type)
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

      public override void VisitType(IType type)
      {
      }

      public override void VisitMultitype(IMultitype multitype)
      {
      }

      public override void VisitDynamicType(IDynamicType dynamicType)
      {
      }

      public override void VisitPointerType(IPointerType pointerType)
      {
      }

      public override void VisitAnonymousType(IAnonymousType anonymousType)
      {
      }
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

      public override ResolveErrorType ErrorType
      {
        get { return ResolveErrorType.NOT_RESOLVED; }
      }

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
