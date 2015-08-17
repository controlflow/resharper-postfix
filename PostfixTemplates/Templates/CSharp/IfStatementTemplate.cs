using JetBrains.Annotations;
using JetBrains.ReSharper.PostfixTemplates.CodeCompletion;
using JetBrains.ReSharper.PostfixTemplates.Contexts.CSharp;
using JetBrains.ReSharper.PostfixTemplates.LookupItems;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.PostfixTemplates.Templates.CSharp
{
  [PostfixTemplate(
    templateName: "if",
    description: "Checks boolean expression to be 'true'",
    example: "if (expr)")]
  public sealed class IfStatementTemplate : BooleanExpressionTemplateBase
  {
    protected override PostfixTemplateInfo TryCreateBooleanInfo(CSharpPostfixExpressionContext expression)
    {
      if (expression.CanBeStatement)
      {
        return new PostfixTemplateInfo("if", expression);
      }

      return null;
    }

    public override PostfixTemplateBehavior CreateBehavior(PostfixTemplateInfo info)
    {
      return new CSharpPostfixIfStatementBehavior(info);
    }

    private sealed class CSharpPostfixIfStatementBehavior : CSharpStatementPostfixTemplateBehavior<IIfStatement>
    {
      public CSharpPostfixIfStatementBehavior([NotNull] PostfixTemplateInfo info) : base(info) { }

      protected override IIfStatement CreateStatement(CSharpElementFactory factory, ICSharpExpression expression)
      {
        // automatically fix 'as'-expression to became 'is'-expression
        var asExpression = expression as IAsExpression;
        if (asExpression != null && asExpression.TypeOperand != null && asExpression.Operand != null)
        {
          expression = factory.CreateExpression("$0 is $1", asExpression.Operand, asExpression.TypeOperand);
        }

        var template = "if($0)" + EmbeddedStatementBracesTemplate;
        return (IIfStatement) factory.CreateStatement(template, expression);
      }
    }
  }
}