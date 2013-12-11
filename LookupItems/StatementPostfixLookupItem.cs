using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems
{
  public abstract class StatementPostfixLookupItem<TStatement> : PostfixLookupItem<TStatement>
    where TStatement : class, ICSharpStatement
  {
    protected StatementPostfixLookupItem(
      [NotNull] string shortcut, [NotNull] PrefixExpressionContext context)
      : base(shortcut, context) { }

    protected override TStatement ExpandPostfix(PrefixExpressionContext expression)
    {
      var psiModule = expression.Parent.ExecutionContext.PsiModule;
      var factory = CSharpElementFactory.GetInstance(psiModule);
      var newStatement = CreateStatement(factory, expression.Expression);

      var targetStatement = PrefixExpressionContext.CalculateCanBeStatement(expression.Expression);
      Assertion.AssertNotNull(targetStatement, "targetStatement != null");
      Assertion.Assert(targetStatement.IsPhysical(), "targetStatement.IsPhysical()");

      return targetStatement.ReplaceBy(newStatement);
    }

    [NotNull] protected abstract TStatement CreateStatement(
      [NotNull] CSharpElementFactory factory, [NotNull] ICSharpExpression expression);
  }
}