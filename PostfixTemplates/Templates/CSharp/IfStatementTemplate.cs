using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
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
  public sealed class IfStatementTemplate : BooleanExpressionTemplateBase, IPostfixTemplate<CSharpPostfixTemplateContext>
  {
    protected override ILookupItem CreateBooleanItem(CSharpPostfixExpressionContext expression)
    {
      if (expression.CanBeStatement)
      {
        return new IfItem(expression);
      }

      return null;
    }

    private sealed class IfItem : StatementPostfixLookupItem<IIfStatement>
    {
      public IfItem([NotNull] CSharpPostfixExpressionContext context) : base("if", context) { }

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