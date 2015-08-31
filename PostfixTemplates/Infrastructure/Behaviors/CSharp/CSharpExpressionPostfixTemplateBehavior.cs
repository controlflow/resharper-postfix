using JetBrains.Annotations;
using JetBrains.ReSharper.PostfixTemplates.Contexts;
using JetBrains.ReSharper.PostfixTemplates.Contexts.CSharp;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.PostfixTemplates.LookupItems
{
  public abstract class CSharpExpressionPostfixTemplateBehavior<TExpression> : PostfixTemplateBehavior
    where TExpression : class, ICSharpExpression
  {
    protected CSharpExpressionPostfixTemplateBehavior([NotNull] PostfixTemplateInfo info) : base(info) { }

    protected override ITreeNode ExpandPostfix(PostfixExpressionContext context)
    {
      var csharpContext = (CSharpPostfixExpressionContext) context;
      var psiModule = csharpContext.PostfixContext.PsiModule;
      var psiServices = psiModule.GetPsiServices();

      var expandedExpression = psiServices.DoTransaction(ExpandCommandName, () =>
      {
        var factory = CSharpElementFactory.GetInstance(psiModule);
        var expression = csharpContext.Expression;

        // todo: get rid of this xtra behavior?
        var operand = expression.GetOperandThroughParenthesis().NotNull("operand != null");

        var newExpression = CreateExpression(factory, operand);

        return expression.ReplaceBy(newExpression);

        // todo: DecorateReplacedExpression()?
      });

      return expandedExpression;
    }

    [NotNull] // todo: pass postfix expression context
    protected abstract TExpression CreateExpression([NotNull] CSharpElementFactory factory, [NotNull] ICSharpExpression expression);

    protected sealed override void AfterComplete(ITextControl textControl, ITreeNode node)
    {
      AfterComplete(textControl, (TExpression) node);
    }

    protected virtual void AfterComplete(ITextControl textControl, TExpression expression)
    {
      var endOffset = expression.GetDocumentRange().TextRange.EndOffset;
      textControl.Caret.MoveTo(endOffset, CaretVisualPlacement.DontScrollIfVisible);
    }
  }
}