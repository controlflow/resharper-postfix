using JetBrains.Annotations;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.Templates
{
  [PostfixTemplate(
    templateName: "await",
    description: "Awaits expressions of 'Task' type",
    example: "await expr")]
  public class AwaitExpressionTemplate : IPostfixTemplate
  {
    public ILookupItem CreateItems(PostfixTemplateContext context)
    {
      var expressionContext = context.InnerExpression;
      var function = context.ContainingFunction;
      if (function == null) return null;

      if (!context.IsForceMode)
      {
        if (!function.IsAsync) return null;

        var expressionType = expressionContext.Type;
        if (!expressionType.IsUnknown)
        {
          if (!(expressionType.IsTask() ||
                expressionType.IsGenericTask())) return null;
        }
      }

      // check expression is not already awaited
      var expression = (context.Reference as IReferenceExpression);
      var unaryExpression = expression.GetContainingParenthesizedExpression();

      var awaitExpression = AwaitExpressionNavigator.GetByTask(unaryExpression as IUnaryExpression);
      if (awaitExpression == null)
      {
        return new AwaitItem(expressionContext);
      }

      return null;
    }

    private sealed class AwaitItem : ExpressionPostfixLookupItem<IAwaitExpression>
    {
      public AwaitItem([NotNull] PrefixExpressionContext context) : base("await", context) { }

      protected override IAwaitExpression CreateExpression(
        CSharpElementFactory factory, ICSharpExpression expression)
      {
        return (IAwaitExpression) factory.CreateExpression("await $0", expression);
      }
    }
  }
}