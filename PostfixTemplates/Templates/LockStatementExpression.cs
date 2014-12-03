using JetBrains.Annotations;
using JetBrains.ReSharper.PostfixTemplates.LookupItems;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.PostfixTemplates.Templates
{
  [PostfixTemplate(
    templateName: "lock",
    description: "Surrounds expression with lock block",
    example: "lock (expr)")]
  public class LockStatementExpression : IPostfixTemplate
  {
    public IPostfixLookupItem CreateItem(PostfixTemplateContext context)
    {
      var expressionContext = context.OuterExpression;
      if (expressionContext == null || !expressionContext.CanBeStatement) return null;

      var expressionType = expressionContext.Type;

      if (context.IsAutoCompletion)
      {
        if (expressionType.IsUnknown) return null;
        if (!expressionType.IsObject()) return null;
      }
      else
      {
        if (expressionType.Classify == TypeClassification.VALUE_TYPE) return null;
      }

      return new LockItem(expressionContext);
    }

    private sealed class LockItem : StatementPostfixLookupItem<ILockStatement>
    {
      public LockItem([NotNull] PrefixExpressionContext context) : base("lock", context) { }

      protected override ILockStatement CreateStatement(CSharpElementFactory factory, ICSharpExpression expression)
      {
        var template = "lock($0)" + EmbeddedStatementBracesTemplate;
        return (ILockStatement) factory.CreateStatement(template, expression);
      }
    }
  }
}