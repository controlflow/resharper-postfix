using JetBrains.Annotations;
using JetBrains.ReSharper.PostfixTemplates.CodeCompletion;
using JetBrains.ReSharper.PostfixTemplates.Contexts.CSharp;
using JetBrains.ReSharper.PostfixTemplates.LookupItems;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.Util;

namespace JetBrains.ReSharper.PostfixTemplates.Templates.CSharp
{
  [PostfixTemplate(
    templateName: "else",
    description: "Checks boolean expression to be 'false'",
    example: "if (!expr)")]
  public class ElseStatementTemplate : BooleanExpressionTemplateBase
  {
    protected override PostfixTemplateInfo TryCreateBooleanInfo(CSharpPostfixExpressionContext expression)
    {
      if (expression.CanBeStatement)
      {
        return new PostfixTemplateInfo("else", expression);
      }

      return null;
    }

    public override PostfixTemplateBehavior CreateBehavior(PostfixTemplateInfo info)
    {
      return new CSharpPostfixInvertedIfStatementBehavior(info);
    }

    private sealed class CSharpPostfixInvertedIfStatementBehavior : CSharpStatementPostfixTemplateBehavior<IIfStatement>
    {
      public CSharpPostfixInvertedIfStatementBehavior([NotNull] PostfixTemplateInfo info) : base(info) { }

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