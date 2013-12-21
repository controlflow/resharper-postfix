using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.TextControl;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.Templates
{
  // todo: if (!boo.not) { }

  [PostfixTemplate(
    templateName: "not",
    description: "Negates boolean expression",
    example: "!expr", WorksOnTypes = true /* don't like it */)]
  public class NotExpressionTemplate : BooleanExpressionTemplateBase, IPostfixTemplate
  {
    protected override ILookupItem CreateBooleanItem(PrefixExpressionContext expression)
    {
      return new LookupItem(expression);
    }

    private sealed class LookupItem : ExpressionPostfixLookupItem<ICSharpExpression>
    {
      public LookupItem([NotNull] PrefixExpressionContext context) : base("not", context) { }

      protected override ICSharpExpression CreateExpression(
        CSharpElementFactory factory, ICSharpExpression expression)
      {
        return CSharpExpressionUtil.CreateLogicallyNegatedExpression(expression) ?? expression;
      }

      protected override void AfterComplete(ITextControl textControl, ICSharpExpression expression)
      {
        // collapse '!!b' into 'b'
        var unary = expression as IUnaryOperatorExpression;
        if (unary != null && unary.UnaryOperatorType == UnaryOperatorType.EXCL)
        {
          var unary2 = UnaryOperatorExpressionNavigator.GetByOperand(unary);
          if (unary2 != null && unary2.UnaryOperatorType == UnaryOperatorType.EXCL)
          {
            expression.GetPsiServices().DoTransaction(
              typeof(NotExpressionTemplate).FullName, () =>
              {
                using (WriteLockCookie.Create())
                {
                  expression = unary2.ReplaceBy(unary.Operand);
                }
              });
          }
        }

        base.AfterComplete(textControl, expression);
      }
    }
  }
}