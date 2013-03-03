using System.Collections.Generic;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.TemplateProviders
{
  [PostfixTemplateProvider("await", "Awaiting expressions of 'Task' type")]
  public class AwaitExpressionTemplateProvider : IPostfixTemplateProvider
  {
    public IEnumerable<PostfixLookupItem> CreateItems(PostfixTemplateAcceptanceContext context)
    {
      var function = context.ContainingFunction;
      if (function == null) yield break;

      if (context.LooseChecks || function.IsAsync)
      if (context.ExpressionType.IsTask() || context.ExpressionType.IsGenericTask())
      {
        // check expression is not already awaited
        var awaitExpression = AwaitExpressionNavigator.GetByTask(
          context.ReferenceExpression.GetContainingParenthesizedExpression() as IUnaryExpression);
        if (awaitExpression == null)
          yield return new PostfixLookupItem("await", "await $EXPR$");
      }
    }
  }
}