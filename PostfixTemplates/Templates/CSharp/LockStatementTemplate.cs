using JetBrains.Annotations;
using JetBrains.ReSharper.PostfixTemplates.CodeCompletion;
using JetBrains.ReSharper.PostfixTemplates.Contexts.CSharp;
using JetBrains.ReSharper.PostfixTemplates.LookupItems;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.PostfixTemplates.Templates.CSharp
{
  [PostfixTemplate(
    templateName: "lock",
    description: "Surrounds expression with lock block",
    example: "lock (expr)")]
  public class LockStatementTemplate : IPostfixTemplate<CSharpPostfixTemplateContext>
  {
    public PostfixTemplateInfo TryCreateInfo(CSharpPostfixTemplateContext context)
    {
      var expressionContext = context.OuterExpression;
      if (expressionContext == null || !expressionContext.CanBeStatement) return null;

      var expressionType = expressionContext.Type;

      if (context.IsPreciseMode)
      {
        if (expressionType.IsUnknown || !expressionType.IsObject()) return null;
      }
      else
      {
        if (expressionType.Classify == TypeClassification.VALUE_TYPE) return null;
      }

      return new PostfixTemplateInfo("lock", expressionContext);
    }

    public PostfixTemplateBehavior CreateBehavior(PostfixTemplateInfo info)
    {
      return new CSharpPostfixLockStatementBehavior(info);
    }

    private sealed class CSharpPostfixLockStatementBehavior : CSharpStatementPostfixTemplateBehavior<ILockStatement>
    {
      public CSharpPostfixLockStatementBehavior([NotNull] PostfixTemplateInfo info) : base(info) { }

      protected override ILockStatement CreateStatement(CSharpElementFactory factory, ICSharpExpression expression)
      {
        var template = "lock($0)" + EmbeddedStatementBracesTemplate;
        return (ILockStatement) factory.CreateStatement(template, expression);
      }
    }
  }
}