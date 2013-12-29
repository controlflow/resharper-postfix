using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.PostfixTemplates.LookupItems;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.PostfixTemplates.Templates
{
  [PostfixTemplate(
    templateName: "par",
    description: "Parenthesizes current expression",
    example: "(expr)")]
  public class ParenthesizedExpressionTemplate : IPostfixTemplate
  {
    public ILookupItem CreateItem(PostfixTemplateContext context)
    {
      PrefixExpressionContext bestContext = null;
      foreach (var expressionContext in context.Expressions.Reverse())
      {
        if (CommonUtils.IsNiceExpression(expressionContext.Expression))
        {
          bestContext = expressionContext;
          break;
        }
      }

      // available in auto over cast expressions
      var targetContext = bestContext ?? context.OuterExpression;
      var insideCastExpression = CastExpressionNavigator.GetByOp(targetContext.Expression) != null;

      if (!insideCastExpression && context.IsAutoCompletion) return null;

      return new ParenthesesItem(targetContext);
    }

    private sealed class ParenthesesItem : ExpressionPostfixLookupItem<ICSharpExpression>
    {
      public ParenthesesItem([NotNull] PrefixExpressionContext context) : base("par", context) { }

      protected override ICSharpExpression CreateExpression(CSharpElementFactory factory,
                                                            ICSharpExpression expression)
      {
        return factory.CreateExpression("($0)", expression);
      }
    }
  }
}