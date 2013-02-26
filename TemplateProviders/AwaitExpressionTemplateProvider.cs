using System.Collections.Generic;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.TemplateProviders
{
  [PostfixTemplateProvider("await", "Awaiting expressions of type Task inside async methods")]
  public class AwaitExpressionTemplateProvider : IPostfixTemplateProvider
  {
    public IEnumerable<PostfixLookupItem> CreateItems(
      IReferenceExpression referenceExpression, ICSharpExpression expression, IType expressionType, bool canBeStatement)
    {
      var function = referenceExpression.GetContainingNode<ICSharpFunctionDeclaration>();
      if (function == null || !function.IsAsync) yield break;

      if (expressionType.IsTask() || expressionType.IsGenericTask())
      {
        // check expression is not already awaited
        var awaitExpression = AwaitExpressionNavigator.GetByTask(
          referenceExpression.GetContainingParenthesizedExpression() as IUnaryExpression);
        if (awaitExpression == null)
          yield return new PostfixLookupItem("await", "await $EXPR$");
      }
    }
  }
}