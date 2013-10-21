using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
#if RESHARPER7
using JetBrains.ReSharper.Psi;
#endif
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.TemplateProviders
{
  // todo: if (!boo.not) { }

  [PostfixTemplateProvider(
    templateName: "not",
    description: "Negates boolean expression",
    example: "!expr", WorksOnTypes = true /* don't like it */)]
  public class NotExpressionTemplateProvider : BooleanExpressionProviderBase, IPostfixTemplateProvider
  {
    protected override bool CreateBooleanItems(
      PrefixExpressionContext expression, ICollection<ILookupItem> consumer)
    {
      consumer.Add(new LookupItem(expression));
      return true;
    }

    private sealed class LookupItem : ExpressionPostfixLookupItem<ICSharpExpression>
    {
      public LookupItem([NotNull] PrefixExpressionContext context) : base("not", context) { }

      protected override ICSharpExpression CreateExpression(
        CSharpElementFactory factory, ICSharpExpression expression)
      {
        return CSharpExpressionUtil.CreateLogicallyNegatedExpression(expression) ?? expression;
      }

      protected override void AfterComplete(
        ITextControl textControl, Suffix suffix, ICSharpExpression expression, int? caretPosition)
      {
        // collapse '!!b' into 'b'
        var unary = expression as IUnaryOperatorExpression;
        if (unary != null && unary.UnaryOperatorType == UnaryOperatorType.EXCL)
        {
          var unary2 = UnaryOperatorExpressionNavigator.GetByOperand(unary);
          if (unary2 != null && unary2.UnaryOperatorType == UnaryOperatorType.EXCL)
          {
            expression.GetPsiServices().DoTransaction(
              typeof(NotExpressionTemplateProvider).FullName, () =>
            {
              using (WriteLockCookie.Create())
              {
                expression = unary2.ReplaceBy(unary.Operand);
              }
            });
          }
        }

        if (caretPosition == null)
          caretPosition = expression.GetDocumentRange().TextRange.EndOffset;

        base.AfterComplete(textControl, suffix, expression, caretPosition);
      }
    }
  }
}