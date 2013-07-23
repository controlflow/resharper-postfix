using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.TemplateProviders
{
  public abstract class BooleanExpressionProviderBase
  {
    public void CreateItems(PostfixTemplateAcceptanceContext context, ICollection<ILookupItem> consumer)
    {
      foreach (var expressionContext in context.Expressions)
      {
        var expression = expressionContext.Expression;
        if (expressionContext.Type.IsBool()
          || expression is IRelationalExpression
          || expression is IEqualityExpression
          || expression is IConditionalAndExpression
          || expression is IConditionalOrExpression
          || expression is IUnaryOperatorExpression)
        {
          if (CreateBooleanItems(expressionContext, consumer)) return;
        }
      }

      if (context.ForceMode)
      {
        foreach (var expressionContext in context.Expressions)
        {
          if (CreateBooleanItems(expressionContext, consumer)) return;
        }
      }
    }

    protected abstract bool CreateBooleanItems(
      [NotNull] PrefixExpressionContext expression, [NotNull] ICollection<ILookupItem> consumer);
  }
}