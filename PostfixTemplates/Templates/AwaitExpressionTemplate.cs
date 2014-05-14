using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.PostfixTemplates.LookupItems;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;

namespace JetBrains.ReSharper.PostfixTemplates.Templates
{
  // todo: check with .ConfigureAwait()

  [PostfixTemplate(
    templateName: "await",
    description: "Awaits expressions of 'Task' type",
    example: "await expr")]
  public class AwaitExpressionTemplate : IPostfixTemplate
  {
    public IPostfixLookupItem CreateItem(PostfixTemplateContext context)
    {
      var expressionContext = context.InnerExpression;
      if (expressionContext == null) return null;

      var containingFunction = context.ContainingFunction;
      if (containingFunction == null) return null;

      if (context.IsAutoCompletion)
      {
        if (!containingFunction.IsAsync) return null;

        var type = expressionContext.Type;
        if (type.IsUnknown || type.IsTask() || type.IsGenericTask()) { }
        else return null;
      }

      if (IsAlreadyAwaited(expressionContext)) return null;

      return new AwaitItem(expressionContext);
    }

    private static bool IsAlreadyAwaited([NotNull] PrefixExpressionContext context)
    {
      var outerExpression = context.PostfixContext.GetOuterExpression(context.Expression);
      var expression = outerExpression.GetContainingParenthesizedExpression();

      var task = AwaitExpressionNavigator.GetByTask(expression as IUnaryExpression);
      return task != null;
    }

    private sealed class AwaitItem : ExpressionPostfixLookupItem<IAwaitExpression>
    {
      public AwaitItem([NotNull] PrefixExpressionContext context) : base("await", context) { }

      protected override IAwaitExpression CreateExpression(CSharpElementFactory factory,
                                                           ICSharpExpression expression)
      {
        return (IAwaitExpression) factory.CreateExpression("await $0", expression);
      }
    }
  }
}