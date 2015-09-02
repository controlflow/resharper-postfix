using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.ReSharper.Feature.Services.CodeCompletion;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.PostfixTemplates.Contexts.CSharp;
using JetBrains.ReSharper.PostfixTemplates.LookupItems;
using JetBrains.ReSharper.PostfixTemplates.Settings;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Impl;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.PostfixTemplates.Templates.CSharp
{
  [PostfixTemplate(
    templateName: "throw",
    description: "Throws expression of 'Exception' type",
    example: "throw expr;")]
  public class ThrowStatementTemplate : IPostfixTemplate<CSharpPostfixTemplateContext>
  {
    public PostfixTemplateInfo TryCreateInfo(CSharpPostfixTemplateContext context)
    {
      var expressionContext = context.TypeExpression ?? context.OuterExpression;
      if (expressionContext == null || !expressionContext.CanBeStatement) return null;

      var expression = expressionContext.Expression;

      var referencedType = expressionContext.ReferencedType;
      if (referencedType != null) // 'Exception.throw' case
      {
        if (context.IsPreciseMode && !IsInstantiableExceptionType(referencedType, expression))
          return null;

        return new PostfixTemplateInfo("throw", expressionContext, target: PostfixTemplateTarget.TypeUsage);
      }

      bool needFixWithNew;
      if (CheckExpressionType(expressionContext, out needFixWithNew) || !context.IsPreciseMode)
      {
        var reference = expressionContext.Expression as IReferenceExpression;
        if (reference != null && CSharpPostfixUtis.IsReferenceExpressionsChain(reference))
        {
          return new PostfixTemplateInfo("throw", expressionContext, target: PostfixTemplateTarget.TypeUsage);
        }

        var info = new PostfixTemplateInfo("throw", expressionContext);
        if (needFixWithNew) info.PutData(NeedFixWithNewKey, info);
        return info;
      }

      return null;
    }

    private static bool CheckExpressionType([NotNull] CSharpPostfixExpressionContext expressionContext, out bool needFixWithNew)
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
        if (reference == null || !CSharpPostfixUtis.IsReferenceExpressionsChain(reference)) return false;

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

    public PostfixTemplateBehavior CreateBehavior(PostfixTemplateInfo info)
    {
      if (info.Target == PostfixTemplateTarget.TypeUsage)
        return new CSharpPostfixThrowStatementByTypeUsageBehavior(info);

      return new CSharpPostfixThrowStatementBehavior(info);
    }

    [NotNull] private static readonly Key NeedFixWithNewKey = new Key(typeof(ThrowStatementNavigator).FullName + ".NeedFixWithNew");

    private sealed class CSharpPostfixThrowStatementBehavior : CSharpStatementPostfixTemplateBehavior<IThrowStatement>
    {
      public CSharpPostfixThrowStatementBehavior([NotNull] PostfixTemplateInfo info) : base(info) { }

      protected override IThrowStatement CreateStatement(CSharpElementFactory factory, ICSharpExpression expression)
      {
        var insertNewExpression = Info.GetData(NeedFixWithNewKey) != null;

        var template = insertNewExpression ? "throw new $0;" : "throw $0;";
        return (IThrowStatement) factory.CreateStatement(template, expression.GetText());
      }
    }

    private sealed class CSharpPostfixThrowStatementByTypeUsageBehavior : CSharpStatementPostfixTemplateBehavior<IThrowStatement>
    {
      public CSharpPostfixThrowStatementByTypeUsageBehavior([NotNull] PostfixTemplateInfo info) : base(info) { }

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
          var canInstantiate = TypeUtils.CanInstantiateType(statement.Exception.Type(), statement);
          var hasRequiredArguments = (canInstantiate & CanInstantiate.ConstructorWithParameters) != 0;

          var caretNode = hasRequiredArguments ? expression.LPar : (statement.Semicolon ?? (ITreeNode) expression);
          var endOffset = caretNode.GetDocumentRange().TextRange.EndOffset;

          textControl.Caret.MoveTo(endOffset, CaretVisualPlacement.DontScrollIfVisible);

          if (hasRequiredArguments && settingsStore.GetValue(PostfixTemplatesSettingsAccessor.InvokeParameterInfo))
          {
            var lookupItemsOwner = Info.ExecutionContext.LookupItemsOwner;
            LookupUtil.ShowParameterInfo(statement.GetSolution(), textControl, lookupItemsOwner);
          }
        }
      }
    }
  }
}