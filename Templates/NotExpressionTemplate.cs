using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.PostfixTemplates.LookupItems;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.TextControl;

namespace JetBrains.ReSharper.PostfixTemplates.Templates
{
  // todo: if (!boo.not) { }

  [PostfixTemplate(
    templateName: "not",
    description: "Negates boolean expression",
    example: "!expr")]
  public class NotExpressionTemplate : BooleanExpressionTemplateBase, IPostfixTemplate
  {
    protected override ILookupItem CreateBooleanItem(PrefixExpressionContext expression)
    {
      return new NotItem(expression);
    }

    private sealed class NotItem : ExpressionPostfixLookupItem<ICSharpExpression>
    {
      public NotItem([NotNull] PrefixExpressionContext context) : base("not", context) { }

      protected override ICSharpExpression CreateExpression(CSharpElementFactory factory,
                                                            ICSharpExpression expression)
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
            var psiServices = expression.GetPsiServices();
            expression = psiServices.DoTransaction(ExpandCommandName, () =>
            {
              using (WriteLockCookie.Create())
              {
                return unary2.ReplaceBy(unary.Operand);
              }
            });
          }
        }

        base.AfterComplete(textControl, expression);
      }
    }
  }
}