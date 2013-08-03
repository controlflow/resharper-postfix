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
        CSharpElementFactory factory, ICSharpExpression expression)
      {
        return CSharpExpressionUtil.CreateLogicallyNegatedExpression(expression) ?? expression;
      }
    }
  }
}