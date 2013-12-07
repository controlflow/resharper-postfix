using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.TemplateProviders
{
  [PostfixTemplateProvider(
    templateName: "lock",
    description: "Surrounds expression with lock block",
    example: "lock (expr)")]
  public class LockStatementExpression : IPostfixTemplate
  {
    public void CreateItems(
      PostfixTemplateAcceptanceContext context, ICollection<ILookupItem> consumer)
    {
      var exprContext = context.OuterExpression;
      if (!exprContext.CanBeStatement) return;

      var expressionType = exprContext.Type;
      if (expressionType.IsUnknown)
      {
        if (!context.ForceMode) return;
      }
      else
      {
        if (context.ForceMode
          ? expressionType.Classify == TypeClassification.VALUE_TYPE
          : !expressionType.IsObject()) return;
      }

      consumer.Add(new LookupItem(exprContext));
    }

    private sealed class LookupItem : KeywordStatementPostfixLookupItem<ILockStatement>
    {
      public LookupItem([NotNull] PrefixExpressionContext context)
        : base("lock", context) { }

      protected override string Template
      {
        get { return "lock(expr)"; }
      }

      protected override void PlaceExpression(
        ILockStatement statement, ICSharpExpression expression, CSharpElementFactory factory)
      {
        statement.Monitor.ReplaceBy(expression);
      }
    }
  }
}