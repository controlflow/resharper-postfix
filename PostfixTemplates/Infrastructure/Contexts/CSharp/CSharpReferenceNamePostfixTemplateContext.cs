using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI;

namespace JetBrains.ReSharper.PostfixTemplates.Contexts.CSharp
{
  public class CSharpReferenceNamePostfixTemplateContext : CSharpPostfixTemplateContext
  {
    public CSharpReferenceNamePostfixTemplateContext(
      [NotNull] IReferenceName reference, [NotNull] ICSharpExpression expression, [NotNull] PostfixTemplateExecutionContext executionContext)
      : base(reference, expression, executionContext) { }

    private static readonly string FixCommandName = typeof(CSharpReferenceNamePostfixTemplateContext) + ".FixExpression";

    public override CSharpPostfixExpressionContext FixExpression(CSharpPostfixExpressionContext context)
    {
      var expression = context.Expression;
      if (expression.Contains(Reference)) // x is T.bar => x is T
      {
        expression.GetPsiServices().DoTransaction(FixCommandName, () =>
        {
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