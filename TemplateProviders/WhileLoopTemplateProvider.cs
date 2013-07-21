using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.TemplateProviders
{
  [PostfixTemplateProvider("while", "Iterating while boolean statement is 'true'")]
  public sealed class WhileLoopTemplateProvider : BooleanExpressionProviderBase, IPostfixTemplateProvider
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

    private sealed class LookupItem : KeywordStatementPostfixLookupItem<IWhileStatement>
    {
      public LookupItem([NotNull] PrefixExpressionContext context) : base("while", context) { }

      protected override string Template { get { return "while(expr)"; } }
      public override bool ShortcutIsCSharpStatementKeyword { get { return true; } }

      protected override void PlaceExpression(
        IWhileStatement statement, ICSharpExpression expression, CSharpElementFactory factory)
      {
        statement.Condition.ReplaceBy(expression);
      }
    }
  }
}