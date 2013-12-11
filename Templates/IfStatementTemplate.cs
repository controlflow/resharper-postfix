using JetBrains.Annotations;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.TextControl;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.Templates
{
  [PostfixTemplate(
    templateName: "if",
    description: "Checks boolean expression to be 'true'",
    example: "if (expr)", WorksOnTypes = true)]
  public sealed class IfStatementTemplate : BooleanExpressionTemplateBase, IPostfixTemplate
  {
    protected override ILookupItem CreateItem(PrefixExpressionContext expression)
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
        return (IIfStatement) factory.CreateStatement("if($0)" + EmbeddedBracesTemplate, expression);
      }
    }
  }
}