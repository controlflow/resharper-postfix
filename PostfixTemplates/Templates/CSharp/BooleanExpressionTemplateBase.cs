using JetBrains.Annotations;
using JetBrains.ReSharper.PostfixTemplates.CodeCompletion;
using JetBrains.ReSharper.PostfixTemplates.Contexts.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Impl;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.PostfixTemplates.Templates.CSharp
{
  public abstract class BooleanExpressionTemplateBase : IPostfixTemplate<CSharpPostfixTemplateContext>
  {
    public PostfixTemplateInfo TryCreateInfo(CSharpPostfixTemplateContext context)
    {
      var booleanExpressions = new LocalList<CSharpPostfixExpressionContext>();

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

      if (!context.IsPreciseMode && booleanExpressions.Count == 0)
      {
        booleanExpressions.AddRange(context.Expressions);
      }

      if (booleanExpressions.Count > 0)
      {
        return TryCreateBooleanInfo(booleanExpressions.ToArray());
      }

      return null;
    }

    public abstract PostfixTemplateBehavior CreateBehavior(PostfixTemplateInfo info);

    [CanBeNull]
    protected abstract PostfixTemplateInfo TryCreateBooleanInfo([NotNull] CSharpPostfixExpressionContext expression);

    [CanBeNull]
    protected virtual PostfixTemplateInfo TryCreateBooleanInfo([NotNull] CSharpPostfixExpressionContext[] expressions)
    {
      foreach (var expressionContext in expressions)
      {
        var lookupItem = TryCreateBooleanInfo(expressionContext);
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

      if (expression is IIsExpression) return true;

      return false;
    }
  }
}