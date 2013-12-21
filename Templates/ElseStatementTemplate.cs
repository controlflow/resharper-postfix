using JetBrains.Annotations;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.Util;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.Templates
{
  [PostfixTemplate(
    templateName: "else",
    description: "Checks boolean expression to be 'false'",
    example: "if (!expr)", WorksOnTypes = true)]
  public class ElseStatementTemplate : BooleanExpressionTemplateBase, IPostfixTemplate
  {
    protected override ILookupItem CreateItem(PrefixExpressionContext expression)
    {
      if (expression.CanBeStatement)
      {
        return new ElseItem(expression);
      }

      return null;
    }

    private sealed class ElseItem : StatementPostfixLookupItem<IIfStatement>
    {
      public ElseItem([NotNull] PrefixExpressionContext context) : base("else", context) { }

      protected override IIfStatement CreateStatement(
        CSharpElementFactory factory, ICSharpExpression expression)
      {
        var template = "if($0)" + EmbeddedBracesTemplate;
        var statement = (IIfStatement) factory.CreateStatement(template, expression);

        var negated = CSharpExpressionUtil.CreateLogicallyNegatedExpression(statement.Condition);
        statement.Condition.ReplaceBy(negated.NotNull());

        return statement;
      }
    }
  }
}