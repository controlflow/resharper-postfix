using JetBrains.Annotations;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.TemplateProviders
{
  [PostfixTemplate(
    templateName: "if",
    description: "Checks boolean expression to be 'true'",
    example: "if (expr)", WorksOnTypes = true)]
  public sealed class IfStatementTemplate : BooleanExpressionTemplateBase, IPostfixTemplate {
    protected override ILookupItem CreateItem(PrefixExpressionContext expression) {
      if (expression.CanBeStatement) {
        return new IfItem(expression);
      }

      return null;
    }

    private sealed class IfItem : KeywordStatementPostfixLookupItem<IIfStatement> {
      public IfItem([NotNull] PrefixExpressionContext context) : base("if", context) { }

      protected override string Template {
        get { return "if(expr)"; }
      }

      protected override void PlaceExpression(IIfStatement statement,
                                              ICSharpExpression expression,
                                              CSharpElementFactory factory) {
        statement.Condition.ReplaceBy(expression);
      }
    }
  }
}