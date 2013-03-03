using System.Collections.Generic;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.TemplateProviders
{
  [PostfixTemplateProvider("await", "Awaiting expressions of 'Task' type")]
  public class AwaitExpressionTemplateProvider : IPostfixTemplateProvider
  {
    public void CreateItems(PostfixTemplateAcceptanceContext context, ICollection<ILookupItem> consumer)
    {
      var function = context.ContainingFunction;
      if (function == null) return;

      if (context.LooseChecks || function.IsAsync)
      if (context.ExpressionType.IsTask() || context.ExpressionType.IsGenericTask())
      {
        // check expression is not already awaited
        var awaitExpression = AwaitExpressionNavigator.GetByTask(
          context.ReferenceExpression.GetContainingParenthesizedExpression() as IUnaryExpression);
        if (awaitExpression == null)
          consumer.Add(new PostfixLookupItem(context, "await", "await $EXPR$"));
      }
    }
  }
}