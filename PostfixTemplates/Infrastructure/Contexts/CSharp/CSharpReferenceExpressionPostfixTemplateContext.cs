using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.PostfixTemplates.Contexts.CSharp
{
  public class CSharpReferenceExpressionPostfixTemplateContext : CSharpPostfixTemplateContext
  {
    public CSharpReferenceExpressionPostfixTemplateContext(
      [NotNull] IReferenceExpression reference, [NotNull] ICSharpExpression expression, [NotNull] PostfixTemplateExecutionContext executionContext)
      : base(reference, expression, executionContext) { }

    private static readonly string FixCommandName = typeof(CSharpReferenceExpressionPostfixTemplateContext) + ".FixExpression";

    public override CSharpPostfixExpressionContext FixExpression(CSharpPostfixExpressionContext context)
    {
      var referenceExpression = (IReferenceExpression)Reference;

      var expression = context.Expression;
      if (expression.Parent == referenceExpression) // foo.bar => foo
      {
        var psiServices = expression.GetPsiServices();

        var newExpression = psiServices.DoTransaction(FixCommandName, () =>
        {
          return referenceExpression.ReplaceBy(expression);
        });

        Assertion.AssertNotNull(newExpression, "newExpression != null");
        Assertion.Assert(newExpression.IsPhysical(), "newExpression.IsPhysical()");

        return new CSharpPostfixExpressionContext(this, newExpression);
      }

      if (expression.Contains(referenceExpression)) // boo > foo.bar => boo > foo
      {
        var qualifier = referenceExpression.QualifierExpression;
        var psiServices = expression.GetPsiServices();

        var newExpression = psiServices.DoTransaction(FixCommandName, () =>
        {
          return referenceExpression.ReplaceBy(qualifier.NotNull());
        });

        Assertion.AssertNotNull(newExpression, "newExpression != null");
        Assertion.Assert(expression.IsPhysical(), "expression.IsPhysical()");
      }

      return context;
    }

    public override ICSharpExpression GetOuterExpression(ICSharpExpression expression)
    {
      var reference = ReferenceExpressionNavigator.GetByQualifierExpression(expression);
      if (reference != null && reference == Reference)
      {
        return reference;
      }

      return base.GetOuterExpression(expression);
    }
  }
}