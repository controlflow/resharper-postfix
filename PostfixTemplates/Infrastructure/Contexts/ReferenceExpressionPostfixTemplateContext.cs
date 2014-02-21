using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.PostfixTemplates
{
  public class ReferenceExpressionPostfixTemplateContext : PostfixTemplateContext
  {
    public ReferenceExpressionPostfixTemplateContext([NotNull] IReferenceExpression reference,
                                                     [NotNull] ICSharpExpression expression,
                                                     [NotNull] PostfixExecutionContext executionContext)
      : base(reference, expression, executionContext) { }

    private static readonly string FixCommandName =
      typeof(ReferenceExpressionPostfixTemplateContext) + ".FixExpression";

    public override PrefixExpressionContext FixExpression(PrefixExpressionContext context)
    {
      var referenceExpression = (IReferenceExpression)Reference;

      var expression = context.Expression;
      if (expression.Parent == referenceExpression) // foo.bar => foo
      {
        ICSharpExpression newExpression = null;
        expression.GetPsiServices().DoTransaction(FixCommandName,
          () => newExpression = referenceExpression.ReplaceBy(expression));

        Assertion.AssertNotNull(newExpression, "newExpression != null");
        Assertion.Assert(newExpression.IsPhysical(), "newExpression.IsPhysical()");

        return new PrefixExpressionContext(this, newExpression);
      }

      if (expression.Contains(referenceExpression)) // boo > foo.bar => boo > foo
      {
        var qualifier = referenceExpression.QualifierExpression;
        expression.GetPsiServices().DoTransaction(FixCommandName,
          () => referenceExpression.ReplaceBy(qualifier.NotNull()));

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