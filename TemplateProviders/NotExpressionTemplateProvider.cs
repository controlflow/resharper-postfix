using System.Collections.Generic;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Parsing;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.TemplateProviders
{
  [PostfixTemplateProvider("not", "Negates boolean expression")]
  public class NotExpressionTemplateProvider : IPostfixTemplateProvider
  {
    public void CreateItems(PostfixTemplateAcceptanceContext context, ICollection<ILookupItem> consumer)
    {
      if (context.CanBeStatement)
      {
        if (!context.LooseChecks)
        {
          if (context.ExpressionType.IsBool()) return;

          // do not show if expression is already negated
          var unary = UnaryOperatorExpressionNavigator.GetByOperand(
            context.ReferenceExpression.GetContainingParenthesizedExpression() as IUnaryExpression);
          if (unary != null && unary.OperatorSign.GetTokenType() != CSharpTokenType.EXCL) return;
        }

        consumer.Add(new PostfixLookupItem(context, "not", "!$EXPR$"));
      }
    }
  }
}