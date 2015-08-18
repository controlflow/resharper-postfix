using JetBrains.Annotations;
using JetBrains.ReSharper.PostfixTemplates.CodeCompletion;
using JetBrains.ReSharper.PostfixTemplates.Contexts;
using JetBrains.ReSharper.PostfixTemplates.Contexts.CSharp;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.PostfixTemplates.LookupItems
{
  public class CSharpExpressionPostfixTemplateBehavior<TExpression> : PostfixTemplateBehavior
    where TExpression : class, ICSharpExpression
  {
    protected CSharpExpressionPostfixTemplateBehavior([NotNull] PostfixTemplateInfo info) : base(info)
    {
      Assertion.Assert(info.Target == PostfixTemplateTarget.Expression, "info.Target == PostfixTemplateTarget.Expression");
    }

    protected override TExpression ExpandPostfix(CSharpPostfixExpressionContext context)
    {
      var psiModule = context.PostfixContext.PsiModule;
      var expandedExpression = psiModule.GetPsiServices().DoTransaction(ExpandCommandName, () =>
      {
        var factory = CSharpElementFactory.GetInstance(psiModule);
        var expression = context.Expression;
        var operand = expression.GetOperandThroughParenthesis().NotNull("operand != null");

        var newExpression = CreateExpression(factory, operand);

        return expression.ReplaceBy(newExpression);
      });

      return expandedExpression;
    }

    [NotNull]
    protected abstract TExpression CreateExpression([NotNull] CSharpElementFactory factory, [NotNull] ICSharpExpression expression);

    protected override void AfterComplete(ITextControl textControl, TExpression expression)
    {
      var endOffset = expression.GetDocumentRange().TextRange.EndOffset;
      textControl.Caret.MoveTo(endOffset, CaretVisualPlacement.DontScrollIfVisible);
    }
  }
}