using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.Util;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.TemplateProviders
{
  [PostfixTemplateProvider("ifnot", "Checks boolean expression to be 'false'")]
  public class IfNotStatementTemplateProvider : BooleanExpressionProviderBase, IPostfixTemplateProvider
  {
    protected override bool CreateBooleanItems(
      PrefixExpressionContext expression, ICollection<ILookupItem> consumer)
    {
      if (expression.CanBeStatement)
      {
        consumer.Add(new LookupItem(expression));
        return true;
      }

      return false;
    }

    private sealed class LookupItem : KeywordStatementPostfixLookupItem<IIfStatement>
    {
      public LookupItem([NotNull] PrefixExpressionContext context) : base("ifnot", context) { }

      protected override string Template { get { return "if(expr)"; } }

      protected override void PlaceExpression(
        IIfStatement statement, ICSharpExpression expression, CSharpElementFactory factory)
      {
        var physical = statement.Condition.ReplaceBy(expression);
        var negated = CSharpExpressionUtil.CreateLogicallyNegatedExpression(physical);
        statement.Condition.ReplaceBy(negated.NotNull());
      }
    }
  }
}