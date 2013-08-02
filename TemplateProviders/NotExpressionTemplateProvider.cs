using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
#if RESHARPER7
using JetBrains.ReSharper.Psi;
#else
using JetBrains.ReSharper.Psi.Modules;
#endif

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.TemplateProviders
{
  [PostfixTemplateProvider("not", "Negates boolean expression", WorksOnTypes = true)]
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
        IPsiModule psiModule, CSharpElementFactory factory, ICSharpExpression expression)
      {
        return expression;
      }

      protected override ICSharpExpression ProcessExpression(ICSharpExpression expression)
      {
        var negatedExpression = CSharpExpressionUtil.CreateLogicallyNegatedExpression(expression);
        if (negatedExpression == null) return expression;

        return expression.ReplaceBy(negatedExpression);
      }
    }
  }
}