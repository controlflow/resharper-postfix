using JetBrains.Annotations;
using JetBrains.ReSharper.PostfixTemplates.Contexts.CSharp;
using JetBrains.ReSharper.PostfixTemplates.LookupItems;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;

namespace JetBrains.ReSharper.PostfixTemplates.Templates.CSharp
{
  // todo: make it work over anything awaitable?
  // todo: this is all wrong: check for async closures!
  // todo: insert 'async' modifier when needed + wrap type

  [PostfixTemplate(
    templateName: "await",
    description: "Awaits expressions of 'Task' type",
    example: "await expr")]
  public class AwaitExpressionTemplate : IPostfixTemplate<CSharpPostfixTemplateContext>
  {
    public PostfixTemplateInfo TryCreateInfo(CSharpPostfixTemplateContext context)
    {
      var expressionContext = context.InnerExpression;
      if (expressionContext == null) return null;

      var returnValueOwner = context.ContainingReturnValueOwner;
      if (returnValueOwner == null) return null;
      
      // 'await' is not available in initializers/attributes/constructors

      if (context.IsPreciseMode)
      {
        bool isAsync;
        var returnType = ReturnStatementUtil.FindExpectedReturnType(returnValueOwner, out isAsync);

        if (!isAsync) return null;

        var type = expressionContext.Type;
        if (!type.IsUnknown && !IsAwaitableType(type)) return null;
      }

      if (IsAlreadyAwaited(expressionContext)) return null;

      return new PostfixTemplateInfo("await", expressionContext);
    }

    private static bool IsAwaitableType(IType type)
    {
      var declaredType = type as IDeclaredType;
      if (declaredType == null) return false;

      if (declaredType.IsTask()) return true;
      if (declaredType.IsGenericTask()) return true;
      if (declaredType.IsConfigurableAwaitable()) return true;
      if (declaredType.IsGenericConfigurableAwaitable()) return true;

      return false; // todo: can we do better?
    }

    private static bool IsAlreadyAwaited([NotNull] CSharpPostfixExpressionContext context)
    {
      var outerExpression = context.PostfixContext.GetOuterExpression(context.Expression);
      var expression = outerExpression.GetContainingParenthesizedExpression();

      var task = AwaitExpressionNavigator.GetByTask(expression as IUnaryExpression);
      return task != null;
    }

    public PostfixTemplateBehavior CreateBehavior(PostfixTemplateInfo info)
    {
      return new CSharpPostfixAwaitExpressionBehavior(info);
    }

    private sealed class CSharpPostfixAwaitExpressionBehavior : CSharpExpressionPostfixTemplateBehavior<IAwaitExpression>
    {
      public CSharpPostfixAwaitExpressionBehavior([NotNull] PostfixTemplateInfo info) : base(info) { }

      protected override IAwaitExpression CreateExpression(CSharpElementFactory factory, ICSharpExpression expression)
      {
        return (IAwaitExpression) factory.CreateExpression("await $0", expression);
      }

      // todo: decorate with ';' if awaited expression is of void type?
      // todo: check with ';' suffix
    }
  }
}