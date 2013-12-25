using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.PostfixTemplates.LookupItems;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;

namespace JetBrains.ReSharper.PostfixTemplates.Templates
{
  [PostfixTemplate(
    templateName: "await",
    description: "Awaits expressions of 'Task' type",
    example: "await expr")]
  public class AwaitExpressionTemplate : IPostfixTemplate
  {
    public ILookupItem CreateItem(PostfixTemplateContext context)
    {
      var expressionContext = context.InnerExpression;
      var containingFunction = context.ContainingFunction;
      if (containingFunction == null) return null;

      if (context.IsAutoCompletion)
      {
        if (!containingFunction.IsAsync) return null;

        var expressionType = expressionContext.Type;
        if (!expressionType.IsUnknown)
        {
          if (!(expressionType.IsTask() ||
                expressionType.IsGenericTask())) return null;
        }
      }

      // check expression is not already awaited
      var referenceExpression = context.Reference as IReferenceExpression;
      var expression = referenceExpression.GetContainingParenthesizedExpression();
      var task = AwaitExpressionNavigator.GetByTask(expression as IUnaryExpression);
      if (task != null) return null;

      return new AwaitItem(expressionContext);
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