using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems
{
  public abstract class StatementPostfixLookupItem<TStatement> : PostfixLookupItem
    where TStatement : class, ICSharpStatement
  {
    protected StatementPostfixLookupItem(
      [NotNull] string shortcut, [NotNull] PrefixExpressionContext context)
      : base(shortcut, context) { }

    protected override bool RemoveSemicolon
    {
      get { return true; }
    }

    protected override void ExpandPostfix(
      ITextControl textControl, Suffix suffix, ISolution solution,
      IPsiModule psiModule, PrefixExpressionContext expression)
    {
      var factory = CSharpElementFactory.GetInstance(psiModule);
      var newStatement = CreateStatement(factory, expression.Expression);

      var statement = PrefixExpressionContext.CalculateCanBeStatement(expression.Expression);
      Assertion.AssertNotNull(statement, "TODO");

      statement.ReplaceBy(newStatement);
    }

    [NotNull] protected abstract TStatement CreateStatement(
      [NotNull] CSharpElementFactory factory, [NotNull] ICSharpExpression expression);
  }
}