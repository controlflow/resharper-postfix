using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Parsing;
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
        if (expressionContext.Type.IsBool() || IsBooleanExpression(expression))
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

    private static bool IsBooleanExpression([CanBeNull] ICSharpExpression expression)
    {
      // 'List<int.> xs = ...' case
      var relationalExpression = expression as IRelationalExpression;
      if (relationalExpression != null)
      {
        var operatorSign = relationalExpression.OperatorSign;
        if (operatorSign == null || operatorSign.GetTokenType() != CSharpTokenType.LT) return true;

        var left = relationalExpression.LeftOperand as IReferenceExpression;
        if (left != null && left.Reference.Resolve().DeclaredElement is ITypeElement) return false;

        var right = relationalExpression.LeftOperand as IReferenceExpression;
        if (right != null && right.Reference.Resolve().DeclaredElement is ITypeElement) return false;

        return true;
      }

      return expression is IEqualityExpression
          || expression is IConditionalAndExpression
          || expression is IConditionalOrExpression
          || expression is IUnaryOperatorExpression
          || expression is IIsExpression;
    }

    protected abstract bool CreateBooleanItems(
      [NotNull] PrefixExpressionContext expression, [NotNull] ICollection<ILookupItem> consumer);
  }
}