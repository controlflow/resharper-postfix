using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi.CSharp.Impl;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.PostfixTemplates.Templates
{
  public abstract class BooleanExpressionTemplateBase
  {
    public ILookupItem CreateItem(PostfixTemplateContext context)
    {
      var booleanExpressions = new LocalList<PrefixExpressionContext>();

      var booleanType = context.Reference.GetPredefinedType().Bool;
      if (booleanType.IsResolved)
      {
        var conversionRule = context.Reference.GetTypeConversionRule();

        foreach (var expressionContext in context.Expressions)
        {
          var expressionType = expressionContext.ExpressionType;
          if (expressionType.IsResolved)
          {
            if (!expressionType.IsImplicitlyConvertibleTo(booleanType, conversionRule) &&
                !IsBooleanExpressionEx(expressionContext.Expression)) continue;
          }
          else
          {
            if (!IsBooleanExpressionEx(expressionContext.Expression)) continue;
          }

          booleanExpressions.Add(expressionContext);
        }
      }

      if (!context.IsAutoCompletion && booleanExpressions.Count == 0)
      {
        booleanExpressions.AddRange(context.Expressions);
      }

      if (booleanExpressions.Count > 0)
      {
        return CreateBooleanItem(booleanExpressions.ToArray());
      }

      return null;
    }

    [CanBeNull]
    protected abstract ILookupItem CreateBooleanItem([NotNull] PrefixExpressionContext expression);

    [CanBeNull]
    protected virtual ILookupItem CreateBooleanItem([NotNull] PrefixExpressionContext[] expressions)
    {
      foreach (var expressionContext in expressions)
      {
        var lookupItem = CreateBooleanItem(expressionContext);
        if (lookupItem != null) return lookupItem;
      }

      return null;
    }

    private static bool IsBooleanExpressionEx([NotNull] ICSharpExpression expression)
    {
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