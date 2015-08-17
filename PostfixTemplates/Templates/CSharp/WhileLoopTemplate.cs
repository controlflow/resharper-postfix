using JetBrains.Annotations;
using JetBrains.ReSharper.PostfixTemplates.CodeCompletion;
using JetBrains.ReSharper.PostfixTemplates.Contexts.CSharp;
using JetBrains.ReSharper.PostfixTemplates.LookupItems;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.PostfixTemplates.Templates.CSharp
{
  [PostfixTemplate(
    templateName: "while",
    description: "Iterating while boolean statement is 'true'",
    example: "while (expr)")]
  public sealed class WhileLoopTemplate : BooleanExpressionTemplateBase
  {
    protected override PostfixTemplateInfo TryCreateBooleanInfo(CSharpPostfixExpressionContext expression)
    {
      if (expression.CanBeStatement)
      {
        return new PostfixTemplateInfo("while", expression);
      }

      return null;
    }

    public override PostfixTemplateBehavior CreateBehavior(PostfixTemplateInfo info)
    {
      return new CSharpPostfixWhileStatementBehavior(info);
    }

    private sealed class CSharpPostfixWhileStatementBehavior : CSharpStatementPostfixTemplateBehavior<IWhileStatement>
    {
      public CSharpPostfixWhileStatementBehavior([NotNull] PostfixTemplateInfo info) : base(info) { }

      protected override IWhileStatement CreateStatement(CSharpElementFactory factory, ICSharpExpression expression)
      {
        var template = "while($0)" + EmbeddedStatementBracesTemplate;
        return (IWhileStatement) factory.CreateStatement(template, expression);
      }
    }
  }
}