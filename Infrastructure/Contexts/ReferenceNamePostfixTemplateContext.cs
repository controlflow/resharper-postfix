using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion
{
  public class ReferenceNamePostfixTemplateContext : PostfixTemplateContext
  {
    public ReferenceNamePostfixTemplateContext(
      [NotNull] IReferenceName reference, [NotNull] ICSharpExpression expression,
      [NotNull] PostfixExecutionContext executionContext)
      : base(reference, expression, executionContext) { }

    public override PrefixExpressionContext FixExpression(PrefixExpressionContext context)
    {
      var referenceName = (IReferenceName)Reference;

      var expression = context.Expression;
      if (expression.Contains(referenceName)) // x is T.bar => x is T
      {
        var qualifier = referenceName.Qualifier.NotNull();
        var newExpression = referenceName.ReplaceBy(qualifier);
      }

      return context;
    }
  }
}