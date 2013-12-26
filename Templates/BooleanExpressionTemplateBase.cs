using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.PostfixTemplates.Templates
{
  public abstract class BooleanExpressionTemplateBase
  {
    public ILookupItem CreateItem(PostfixTemplateContext context)
    {
      foreach (var expressionContext in context.Expressions)
      {
        if (!IsBooleanExpression(expressionContext)) continue;

        var lookupItem = CreateBooleanItem(expressionContext);
        if (lookupItem == null) continue;

        return lookupItem;
      }

      if (!context.IsAutoCompletion)
      {
        foreach (var expressionContext in context.Expressions)
        {
          var lookupItem = CreateBooleanItem(expressionContext);
          if (lookupItem == null) continue;

          return lookupItem;
        }
      }

      return null;
    }

    [CanBeNull]
    protected abstract ILookupItem CreateBooleanItem([NotNull] PrefixExpressionContext expression);

    private static bool IsBooleanExpression([NotNull] PrefixExpressionContext context)
    {
      if (context.Type.IsBool()) return true;

      var expression = context.Expression;

      var binaryExpression = expression as IBinaryExpression;
      if (binaryExpression != null)
      {
        return binaryExpression is IRelationalExpression
            || binaryExpression is IEqualityExpression
            || binaryExpression is IConditionalAndExpression
            || binaryExpression is IConditionalOrExpression;
      }

      var unaryExpression = expression as IUnaryOperatorExpression;
      if (unaryExpression != null)
      {
        return (unaryExpression.UnaryOperatorType == UnaryOperatorType.EXCL);
      }

      return (expression is IIsExpression);
    }
  }
}