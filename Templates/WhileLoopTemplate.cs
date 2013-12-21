using JetBrains.Annotations;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.Templates
{
  [PostfixTemplate(
    templateName: "while",
    description: "Iterating while boolean statement is 'true'",
    example: "while (expr)", WorksOnTypes = true)]
  public sealed class WhileLoopTemplate : BooleanExpressionTemplateBase, IPostfixTemplate
  {
    protected override ILookupItem CreateBooleanItem(PrefixExpressionContext expression)
    {
      if (expression.CanBeStatement)
      {
        return new WhileItem(expression);
      }

      return null;
    }

    private sealed class WhileItem : StatementPostfixLookupItem<IWhileStatement>
    {
      public WhileItem([NotNull] PrefixExpressionContext context) : base("while", context) { }

      protected override IWhileStatement CreateStatement(
        CSharpElementFactory factory, ICSharpExpression expression)
      {
        var template = "while($0)" + EmbeddedStatementBracesTemplate;
        return (IWhileStatement) factory.CreateStatement(template, expression);
      }
    }
  }
}