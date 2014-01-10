using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Impl;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.PostfixTemplates.Templates
{
  public abstract class BooleanExpressionTemplateBase
  {
    public ILookupItem CreateItem(PostfixTemplateContext context)
    {
      var booleanType = context.Reference.GetPredefinedType().Bool;
      if (booleanType.IsResolved)
      {
        var conversionRule = context.Reference.GetTypeConversionRule();

        foreach (var expressionContext in context.Expressions)
        {
          var expressionType = expressionContext.ExpressionType;
          if (!expressionType.IsResolved) continue;

          if (expressionType.IsImplicitlyConvertibleTo(booleanType, conversionRule) ||
              IsBooleanExpressionEx(expressionContext.Expression))
          {
            var lookupItem = CreateBooleanItem(expressionContext);
            if (lookupItem != null)
            {
              return lookupItem;
            }
          }
        }
      }

      if (!context.IsAutoCompletion)
      {
        foreach (var expressionContext in context.Expressions)
        {
          var lookupItem = CreateBooleanItem(expressionContext);
          if (lookupItem != null)
          {
            return lookupItem;
          }
        }
      }

      return null;
    }

    [CanBeNull]
    protected abstract ILookupItem CreateBooleanItem([NotNull] PrefixExpressionContext expression);

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