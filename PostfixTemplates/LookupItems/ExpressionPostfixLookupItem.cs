using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.PostfixTemplates.LookupItems
{
  public abstract class ExpressionPostfixLookupItem<TExpression> : PostfixLookupItem<TExpression>
    where TExpression : class, ICSharpExpression
  {
    protected ExpressionPostfixLookupItem([NotNull] string shortcut, [NotNull] PrefixExpressionContext context)
      : base(shortcut, context) { }

    protected ExpressionPostfixLookupItem([NotNull] string shortcut, [NotNull] PrefixExpressionContext[] contexts)
      : base(shortcut, contexts) { }

    protected override TExpression ExpandPostfix(PrefixExpressionContext context)
    {
      var psiModule = context.PostfixContext.PsiModule;
      var expandedExpression = psiModule.GetPsiServices().DoTransaction(ExpandCommandName, () =>
      {
        var factory = CSharpElementFactory.GetInstance(psiModule);
        var expression = context.Expression;
        var newExpression = CreateExpression(
          factory, expression.GetOperandThroughParenthesis().NotNull());

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