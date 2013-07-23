using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.Modules;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.TemplateProviders
{
  [PostfixTemplateProvider("await", "Awaiting expressions of 'Task' type")]
  public class AwaitExpressionTemplateProvider : IPostfixTemplateProvider
  {
    public void CreateItems(PostfixTemplateAcceptanceContext context, ICollection<ILookupItem> consumer)
    {
      var exprContext = context.PossibleExpressions.FirstOrDefault();
      if (exprContext == null) return;

      var function = context.ContainingFunction;
      if (function == null) return;
  
      if (context.ForceMode || function.IsAsync)
      {
        if (exprContext.Type.IsTask() || exprContext.Type.IsGenericTask() || context.ForceMode)
        {
          // check expression is not already awaited
          var awaitExpression = AwaitExpressionNavigator.GetByTask(
            context.PostfixReferenceExpression.GetContainingParenthesizedExpression() as IUnaryExpression);

          if (awaitExpression == null)
            consumer.Add(new LookupItem(exprContext));
        }
      }
    }

    private sealed class LookupItem : ExpressionPostfixLookupItem<IAwaitExpression>
    {
      public LookupItem([NotNull] PrefixExpressionContext context)
        : base("await", context) { }

      protected override IAwaitExpression CreateExpression(
        IPsiModule psiModule, CSharpElementFactory factory, ICSharpExpression expression)
      {
        return (IAwaitExpression) factory.CreateExpression("await $0", expression);
      }
    }
  }
}