using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI;

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
      var expression = context.Expression;
      if (expression.Contains(Reference)) // x is T.bar => x is T
      {
        expression.GetPsiServices().DoTransaction(FixCommandName, () => {
          var referenceName = (IReferenceName) Reference;
          var qualifier = referenceName.Qualifier;

          LowLevelModificationUtil.DeleteChild(qualifier); // remove first

          return referenceName.ReplaceBy(qualifier);
        });
      }

      return context;
    }
  }
}