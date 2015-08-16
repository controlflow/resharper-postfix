using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.PostfixTemplates.Contexts;
using JetBrains.ReSharper.PostfixTemplates.Contexts.CSharp;
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
      if (expressionContext == null) return null;

      var containingFunction = context.ContainingFunction;
      if (containingFunction == null) return null;

      if (context.IsAutoCompletion)
      {
        if (!containingFunction.IsAsync) return null;

        var type = expressionContext.Type;
        if (!type.IsUnknown && !IsAwaitableType(type)) return null;
      }

      if (IsAlreadyAwaited(expressionContext)) return null;

      return new AwaitItem(expressionContext);
    }

    private static bool IsAwaitableType(IType type)
    {
      var declaredType = type as IDeclaredType;
      if (declaredType == null) return false;

      if (declaredType.IsTask()) return true;
      if (declaredType.IsGenericTask()) return true;
      if (declaredType.IsConfigurableAwaitable()) return true;
      if (declaredType.IsGenericConfigurableAwaitable()) return true;

      return false;
    }

    private static bool IsAlreadyAwaited([NotNull] CSharpPostfixExpressionContext context)
    {
      var outerExpression = context.PostfixContext.GetOuterExpression(context.Expression);
      var expression = outerExpression.GetContainingParenthesizedExpression();

      var task = AwaitExpressionNavigator.GetByTask(expression as IUnaryExpression);
      return task != null;
    }

    private sealed class AwaitItem : ExpressionPostfixLookupItem<IAwaitExpression>
    {
      public AwaitItem([NotNull] CSharpPostfixExpressionContext context) : base("await", context) { }

      protected override IAwaitExpression CreateExpression(CSharpElementFactory factory, ICSharpExpression expression)
      {
        return (IAwaitExpression) factory.CreateExpression("await $0", expression);
      }
    }
  }
}