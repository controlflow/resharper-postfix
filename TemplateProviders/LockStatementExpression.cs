using JetBrains.Annotations;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.TemplateProviders
{
  [PostfixTemplate(
    templateName: "lock",
    description: "Surrounds expression with lock block",
    example: "lock (expr)")]
  public class LockStatementExpression : IPostfixTemplate
  {
    public ILookupItem CreateItems(PostfixTemplateContext context)
    {
      var expressionContext = context.OuterExpression;
      if (!expressionContext.CanBeStatement) return null;

      var expressionType = expressionContext.Type;
      if (expressionType.IsUnknown) {
        if (!context.ForceMode) return null;
      } else {
        if (context.ForceMode
          ? (expressionType.Classify == TypeClassification.VALUE_TYPE)
          : !expressionType.IsObject()) return null;
      }

      return new LookupItem(expressionContext);
    }

    private sealed class LookupItem : KeywordStatementPostfixLookupItem<ILockStatement> {
      public LookupItem([NotNull] PrefixExpressionContext context) : base("lock", context) { }

      protected override string Template {
        get { return "lock(expr)"; }
      }

      protected override void PlaceExpression(ILockStatement statement,
                                              ICSharpExpression expression,
                                              CSharpElementFactory factory) {
        statement.Monitor.ReplaceBy(expression);
      }
    }
  }
}