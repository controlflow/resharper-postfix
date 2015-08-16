using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.PostfixTemplates.Contexts.CSharp;
using JetBrains.ReSharper.PostfixTemplates.LookupItems;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.Util;

namespace JetBrains.ReSharper.PostfixTemplates.Templates
{
  [PostfixTemplate(
    templateName: "else",
    description: "Checks boolean expression to be 'false'",
    example: "if (!expr)")]
  public class ElseStatementTemplate : BooleanExpressionTemplateBase, IPostfixTemplate
  {
    protected override ILookupItem CreateBooleanItem(CSharpPostfixExpressionContext expression)
    {
      if (expression.CanBeStatement)
      {
        return new ElseItem(expression);
      }

      return null;
    }

    private sealed class ElseItem : StatementPostfixLookupItem<IIfStatement>
    {
      public ElseItem([NotNull] CSharpPostfixExpressionContext context) : base("else", context) { }

      protected override IIfStatement CreateStatement(CSharpElementFactory factory, ICSharpExpression expression)
      {
        var template = "if($0)" + EmbeddedStatementBracesTemplate;
        var statement = (IIfStatement) factory.CreateStatement(template, expression);

        var negated = CSharpExpressionUtil.CreateLogicallyNegatedExpression(statement.Condition);
        statement.Condition.ReplaceBy(negated.NotNull());

        return statement;
      }
    }
  }
}