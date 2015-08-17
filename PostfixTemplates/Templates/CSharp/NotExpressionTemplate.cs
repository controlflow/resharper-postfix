using System;
using JetBrains.Annotations;
using JetBrains.ReSharper.PostfixTemplates.CodeCompletion;
using JetBrains.ReSharper.PostfixTemplates.Contexts.CSharp;
using JetBrains.ReSharper.PostfixTemplates.LookupItems;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.TextControl;

namespace JetBrains.ReSharper.PostfixTemplates.Templates.CSharp
{
  [PostfixTemplate(
    templateName: "not",
    description: "Negates boolean expression",
    example: "!expr")]
  public class NotExpressionTemplate : BooleanExpressionTemplateBase
  {
    protected override PostfixTemplateInfo TryCreateBooleanInfo(CSharpPostfixExpressionContext expression)
    {
      throw new InvalidOperationException("Should not be called");
    }

    protected override PostfixTemplateInfo TryCreateBooleanInfo(CSharpPostfixExpressionContext[] expressions)
    {
      if (expressions.Length > 1)
      {
        expressions = Array.FindAll(expressions, IsNotUnderUnaryNegation);
      }

      return new PostfixTemplateInfo("not", expressions);
    }

    private static bool IsNotUnderUnaryNegation([NotNull] CSharpPostfixExpressionContext context)
    {
      var unaryExpression = context.ExpressionWithReference as IUnaryExpression;

      var operatorExpression = UnaryOperatorExpressionNavigator.GetByOperand(unaryExpression);
      if (operatorExpression == null) return true;

      return operatorExpression.UnaryOperatorType != UnaryOperatorType.EXCL;
    }

    public override PostfixTemplateBehavior CreateBehavior(PostfixTemplateInfo info)
    {
      return new CSharpPostfixNegationBehavior(info);
    }

    private sealed class CSharpPostfixNegationBehavior : CSharpExpressionPostfixTemplateBehavior<ICSharpExpression>
    {
      public CSharpPostfixNegationBehavior([NotNull] PostfixTemplateInfo info) : base(info) { }

      protected override string ExpressionSelectTitle
      {
        get { return "Select expression to invert"; }
      }

      protected override ICSharpExpression CreateExpression(CSharpElementFactory factory, ICSharpExpression expression)
      {
        return CSharpExpressionUtil.CreateLogicallyNegatedExpression(expression);
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
            expression = psiServices.DoTransaction(
              ExpandCommandName, () => unary2.ReplaceBy(unary.Operand));
          }
        }

        base.AfterComplete(textControl, expression);
      }
    }
  }
}