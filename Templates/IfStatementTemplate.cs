using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.PostfixTemplates.LookupItems;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.PostfixTemplates.Templates
{
  [PostfixTemplate(
    templateName: "if",
    description: "Checks boolean expression to be 'true'",
    example: "if (expr)")]
  public sealed class IfStatementTemplate : BooleanExpressionTemplateBase, IPostfixTemplate
  {
    protected override ILookupItem CreateBooleanItem(PrefixExpressionContext expression)
    {
      if (expression.CanBeStatement)
      {
        return new IfItem(expression);
      }

      return null;
    }


    private sealed class IfItem : StatementPostfixLookupItem<IIfStatement>
    {
      public IfItem([NotNull] PrefixExpressionContext context) : base("if", context) { }

      protected override IIfStatement CreateStatement(
        CSharpElementFactory factory, ICSharpExpression expression)
      {
        var template = "if($0)" + EmbeddedStatementBracesTemplate;
        return (IIfStatement) factory.CreateStatement(template, expression);
      }
    }
  }
}