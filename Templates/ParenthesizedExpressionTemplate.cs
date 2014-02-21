using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.PostfixTemplates.LookupItems;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.PostfixTemplates.Templates
{
  // todo: return foo.Bar as T.par do not works (R# bug)

  [PostfixTemplate(
    templateName: "par",
    description: "Parenthesizes current expression",
    example: "(expr)")]
  public class ParenthesizedExpressionTemplate : IPostfixTemplate
  {
    public ILookupItem CreateItem(PostfixTemplateContext context)
    {
      var bestContext = CommonUtils.FindBestExpressionContext(context, expression =>
        CommonUtils.IsNiceExpressionWithValue(expression) && !IsUnlikelyNeedsParenthesizes(expression));

      if (bestContext == null) return null;

      // available in auto over cast expressions
      var castExpression = CastExpressionNavigator.GetByOp(bestContext.Expression);
      var insideCastExpression = (castExpression != null);
      if (!insideCastExpression && context.IsAutoCompletion) return null;

      return new ParenthesesItem(bestContext);
    }

    private static bool IsUnlikelyNeedsParenthesizes([NotNull] ICSharpExpression expression)
    {
      return (expression is IParenthesizedExpression)
        || CastExpressionNavigator.GetByOp(expression) != null
        || IfStatementNavigator.GetByCondition(expression) != null
        || WhileStatementNavigator.GetByCondition(expression) != null
        || DoStatementNavigator.GetByCondition(expression) != null
        || UsingStatementNavigator.GetByExpression(expression) != null
        || LockStatementNavigator.GetByMonitor(expression) != null
        || CheckedExpressionNavigator.GetByOperand(expression) != null
        || UncheckedExpressionNavigator.GetByOperand(expression) != null
        || ParenthesizedExpressionNavigator.GetByExpression(expression) != null;
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