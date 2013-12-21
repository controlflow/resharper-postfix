using JetBrains.Annotations;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.Templates
{
  [PostfixTemplate(
    templateName: "lock",
    description: "Surrounds expression with lock block",
    example: "lock (expr)")]
  public class LockStatementExpression : IPostfixTemplate
  {
    public ILookupItem CreateItem(PostfixTemplateContext context)
    {
      var expressionContext = context.OuterExpression;
      if (!expressionContext.CanBeStatement) return null;

      var expressionType = expressionContext.Type;
      if (expressionType.IsUnknown)
      {
        if (!context.IsForceMode) return null;
      }
      else
      {
        if (context.IsForceMode
          ? (expressionType.Classify == TypeClassification.VALUE_TYPE)
          : !expressionType.IsObject()) return null;
      }

      return new LookupItem(expressionContext);
    }

    private sealed class LookupItem : StatementPostfixLookupItem<ILockStatement>
    {
      public LookupItem([NotNull] PrefixExpressionContext context) : base("lock", context) { }

      protected override ILockStatement CreateStatement(CSharpElementFactory factory, ICSharpExpression expression)
      {
        var template = "lock($0)" + EmbeddedStatementBracesTemplate;
        return (ILockStatement) factory.CreateStatement(template, expression);
      }
    }
  }
}