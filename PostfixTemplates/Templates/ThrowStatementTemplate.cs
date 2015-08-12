using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.ReSharper.Feature.Services.CodeCompletion;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.PostfixTemplates.LookupItems;
using JetBrains.ReSharper.PostfixTemplates.Settings;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Impl;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;

namespace JetBrains.ReSharper.PostfixTemplates.Templates
{
  [PostfixTemplate(
    templateName: "throw",
    description: "Throws expression of 'Exception' type",
    example: "throw expr;")]
  public class ThrowStatementTemplate : IPostfixTemplate
  {
    public ILookupItem CreateItem(PostfixTemplateContext context)
    {
      var expressionContext = context.TypeExpression ?? context.OuterExpression;
      if (expressionContext == null || !expressionContext.CanBeStatement) return null;

      var expression = expressionContext.Expression;

      var referencedType = expressionContext.ReferencedType;
      if (referencedType != null) // 'Exception.throw' case
      {
        if (context.IsAutoCompletion && !IsInstantiableExceptionType(referencedType, expression))
          return null;

        var canInstantiate = TypeUtils.CanInstantiateType(referencedType, expression);
        var hasParameters = (canInstantiate & CanInstantiate.ConstructorWithParameters) != 0;

        return new ThrowByTypeItem(expressionContext, hasParameters);
      }

      bool needFixWithNew;
      if (CheckExpressionType(expressionContext, out needFixWithNew) || !context.IsAutoCompletion)
      {
        var reference = expressionContext.Expression as IReferenceExpression;
        if (reference != null && CommonUtils.IsReferenceExpressionsChain(reference))
        {
          return new ThrowByTypeItem(expressionContext, hasRequiredArguments: true);
        }

        return new ThrowExpressionItem(expressionContext, needFixWithNew);
      }

      return null;
    }

    private static bool CheckExpressionType([NotNull] PrefixExpressionContext expressionContext, out bool needFixWithNew)
    {
      needFixWithNew = false;

      // 'new Exception().throw' case
      var expressionType = expressionContext.ExpressionType;
      if (expressionType.IsResolved)
      {
        var predefinedType = expressionContext.Expression.GetPredefinedType();
        var conversionRule = expressionContext.Expression.GetTypeConversionRule();
        return expressionType.IsImplicitlyConvertibleTo(predefinedType.Exception, conversionRule);
      }

      // 'Exception(message).new' case
      var invocationExpression = expressionContext.Expression as IInvocationExpression;
      if (invocationExpression != null)
      {
        var reference = invocationExpression.InvokedExpression as IReferenceExpression;
        if (reference == null || !CommonUtils.IsReferenceExpressionsChain(reference)) return false;

        var resolveResult = reference.Reference.Resolve().Result;
        var typeElement = resolveResult.DeclaredElement as ITypeElement;
        if (typeElement == null) return false;

        var declaredType = TypeFactory.CreateType(typeElement, resolveResult.Substitution);
        if (IsInstantiableExceptionType(declaredType, expressionContext.Expression))
        {
          needFixWithNew = true;
          return true;
        }
      }

      return false;
    }

    private static bool IsInstantiableExceptionType([NotNull] IDeclaredType declaredType, [NotNull] ICSharpExpression context)
    {
      var predefinedType = context.GetPredefinedType();
      var conversionRule = context.GetTypeConversionRule();

      return conversionRule.IsImplicitlyConvertibleTo(declaredType, predefinedType.Exception)
          && TypeUtils.CanInstantiateType(declaredType, context) != CanInstantiate.No;
    }

    private sealed class ThrowExpressionItem : StatementPostfixLookupItem<IThrowStatement>
    {
      private readonly bool myInsertNewExpression;

      public ThrowExpressionItem([NotNull] PrefixExpressionContext context, bool insertNewExpression)
        : base("throw", context)
      {
        myInsertNewExpression = insertNewExpression;
      }

      protected override IThrowStatement CreateStatement(CSharpElementFactory factory, ICSharpExpression expression)
      {
        var template = myInsertNewExpression ? "throw new $0;" : "throw $0;";
        return (IThrowStatement) factory.CreateStatement(template, expression.GetText());
      }
    }

    private sealed class ThrowByTypeItem : StatementPostfixLookupItem<IThrowStatement>
    {
      [NotNull] private readonly ILookupItemsOwner myLookupItemsOwner;
      private readonly bool myHasRequiredArguments;

      public ThrowByTypeItem([NotNull] PrefixExpressionContext context, bool hasRequiredArguments)
        : base("throw", context)
      {
        var executionContext = context.PostfixContext.ExecutionContext;
        myLookupItemsOwner = executionContext.LookupItemsOwner;
        myHasRequiredArguments = hasRequiredArguments;
      }

      protected override IThrowStatement CreateStatement(CSharpElementFactory factory, ICSharpExpression expression)
      {
        var settingsStore = expression.GetSettingsStore();
        var parenthesesType = settingsStore.GetValue(CodeCompletionSettingsAccessor.ParenthesesInsertType);
        var parenthesesTemplate = parenthesesType.GetParenthesesTemplate(atStatementEnd: true);

        var template = string.Format("throw new {0}{1}", expression.GetText(), parenthesesTemplate);
        return (IThrowStatement) factory.CreateStatement(template, expression.GetText());
      }

      protected override void AfterComplete(ITextControl textControl, IThrowStatement statement)
      {
        if (statement.Semicolon != null) FormatStatementOnSemicolon(statement);

        var settingsStore = statement.GetSettingsStore();
        var expression = (IObjectCreationExpression) statement.Exception;

        var parenthesesType = settingsStore.GetValue(CodeCompletionSettingsAccessor.ParenthesesInsertType);
        if (parenthesesType == ParenthesesInsertType.None)
        {
          var endOffset = expression.GetDocumentRange().TextRange.EndOffset;
          textControl.Caret.MoveTo(endOffset, CaretVisualPlacement.DontScrollIfVisible);
        }
        else
        {
          var caretNode = myHasRequiredArguments ? expression.LPar : (statement.Semicolon ?? (ITreeNode) expression);
          var endOffset = caretNode.GetDocumentRange().TextRange.EndOffset;

          textControl.Caret.MoveTo(endOffset, CaretVisualPlacement.DontScrollIfVisible);

          if (myHasRequiredArguments && settingsStore.GetValue(PostfixSettingsAccessor.InvokeParameterInfo))
          {
            LookupUtil.ShowParameterInfo(statement.GetSolution(), textControl, myLookupItemsOwner);
          }
        }
      }
    }
  }
}