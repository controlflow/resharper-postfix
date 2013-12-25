using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.PostfixTemplates
{
  public class ReferenceNamePostfixTemplateContext : PostfixTemplateContext
  {
    public ReferenceNamePostfixTemplateContext([NotNull] IReferenceName reference,
                                               [NotNull] ICSharpExpression expression,
                                               [NotNull] PostfixExecutionContext executionContext)
      : base(reference, expression, executionContext) { }

    private static readonly string FixCommandName =
      typeof(ReferenceNamePostfixTemplateContext) + ".FixExpression";

    public override PrefixExpressionContext FixExpression(PrefixExpressionContext context)
    {
      var referenceName = (IReferenceName)Reference;

      var expression = context.Expression;
      if (expression.Contains(referenceName)) // x is T.bar => x is T
      {
        expression.GetPsiServices().DoTransaction(FixCommandName,
          () => referenceName.ReplaceBy(referenceName.Qualifier));
      }

      return context;
    }
  }
}