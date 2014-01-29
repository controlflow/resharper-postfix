using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.PostfixTemplates.LookupItems;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.PostfixTemplates.Templates
{
  // todo: return foo.Bar as T.par do not works

  [PostfixTemplate(
    templateName: "par",
    description: "Parenthesizes current expression",
    example: "(expr)")]
  public class ParenthesizedExpressionTemplate : IPostfixTemplate
  {
    public ILookupItem CreateItem(PostfixTemplateContext context)
    {
      var bestContext = CommonUtils.FindBestExpressionContext(context);
      if (bestContext == null) return null;

      // available in auto over cast expressions
      var castExpression = CastExpressionNavigator.GetByOp(bestContext.Expression);
      var insideCastExpression = (castExpression != null);
      if (!insideCastExpression && context.IsAutoCompletion) return null;

      return new ParenthesesItem(bestContext);
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