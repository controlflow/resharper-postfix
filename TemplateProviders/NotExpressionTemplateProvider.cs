using System.Collections.Generic;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Parsing;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.TemplateProviders
{
  [PostfixTemplateProvider("not", "Negates boolean expression")]
  public class NotExpressionTemplateProvider : IPostfixTemplateProvider
  {
    public IEnumerable<PostfixLookupItem> CreateItems(
      ICSharpExpression expression, IType expressionType, bool canBeStatement)
    {
      if (canBeStatement && expressionType.IsBool())
      {
        var referenceExpression = ReferenceExpressionNavigator.GetByQualifierExpression(expression);
        if (referenceExpression != null)
        {
          // do not show if expression is already negated
          var unary = UnaryOperatorExpressionNavigator.GetByOperand(
            referenceExpression.GetContainingParenthesizedExpression() as IUnaryExpression);
          if (unary != null && unary.OperatorSign.GetTokenType() != CSharpTokenType.EXCL)
            yield break;

          yield return new PostfixLookupItem("not", "!$EXPR$");
        }
      }
    }
  }
}