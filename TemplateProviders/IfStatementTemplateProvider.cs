using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.TemplateProviders
{
  [PostfixTemplateProvider("if", "Checks boolean expression to be 'true'")]
  public sealed class IfStatementTemplateProvider : BooleanExpressionProviderBase, IPostfixTemplateProvider
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

    private sealed class LookupItem : IfStatementPostfixLookupItemBase
    {
      public LookupItem([NotNull] PrefixExpressionContext context) : base("if", context) { }

      public override bool ShortcutIsCSharpStatementKeyword
      {
        get { return true; }
      }

      protected override void PlaceExpression(
        IIfStatement statement, ICSharpExpression expression, CSharpElementFactory factory)
      {
        statement.Condition.ReplaceBy(expression);
      }
    }
  }
}