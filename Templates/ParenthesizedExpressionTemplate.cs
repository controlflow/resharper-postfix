using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.Templates
{
  // todo: foo as Bar.par - available in auto
  // todo: (Bar) foo.par - available in auto?

  [PostfixTemplate(
    templateName: "par",
    description: "Parenthesizes current expression",
    example: "(expr)")]
  public class ParenthesizedExpressionTemplate : IPostfixTemplate
  {
    public ILookupItem CreateItem(PostfixTemplateContext context)
    {
      if (!context.IsForceMode) return null;

      PrefixExpressionContext bestExpression = null;
      foreach (var expression in context.Expressions.Reverse())
      {
        if (CommonUtils.IsNiceExpression(expression.Expression))
        {
          bestExpression = expression;
          break;
        }
      }

      return new ParenthesesItem(bestExpression ?? context.OuterExpression);
    }

    private sealed class ParenthesesItem : ExpressionPostfixLookupItem<ICSharpExpression>
    {
      public ParenthesesItem([NotNull] PrefixExpressionContext context) : base("par", context) { }

      protected override ICSharpExpression CreateExpression(
        CSharpElementFactory factory, ICSharpExpression expression)
      {
        return factory.CreateExpression("($0)", expression);
      }
    }
  }
}