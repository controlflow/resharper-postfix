using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;

namespace JetBrains.ReSharper.PostfixTemplates.LookupItems
{
  public abstract class ExpressionPostfixLookupItem<TExpression> : PostfixLookupItem<TExpression>
    where TExpression : class, ICSharpExpression
  {
    protected ExpressionPostfixLookupItem(
      [NotNull] string shortcut, [NotNull] PrefixExpressionContext context)
      : base(shortcut, context) { }

    protected override TExpression ExpandPostfix(PrefixExpressionContext context)
    {
      var psiModule = context.Parent.ExecutionContext.PsiModule;
      var expression = psiModule.GetPsiServices().DoTransaction(ExpandCommandName, () =>
      {
        var factory = CSharpElementFactory.GetInstance(psiModule);
        var oldExpression = context.Expression;
        var newExpression = CreateExpression(factory, oldExpression);

        return oldExpression.ReplaceBy(newExpression);
      });

      return expression;
    }

    [NotNull] protected abstract TExpression CreateExpression(
      [NotNull] CSharpElementFactory factory, [NotNull] ICSharpExpression expression);

    protected override void AfterComplete(ITextControl textControl, TExpression expression)
    {
      var endOffset = expression.GetDocumentRange().TextRange.EndOffset;
      textControl.Caret.MoveTo(endOffset, CaretVisualPlacement.DontScrollIfVisible);
    }
  }
}