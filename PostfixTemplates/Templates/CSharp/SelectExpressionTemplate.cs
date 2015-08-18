using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.PostfixTemplates.CodeCompletion;
using JetBrains.ReSharper.PostfixTemplates.Contexts.CSharp;
using JetBrains.ReSharper.PostfixTemplates.LookupItems;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;

namespace JetBrains.ReSharper.PostfixTemplates.Templates.CSharp
{
  [PostfixTemplate(
    templateName: "sel",
    description: "Selects expression in editor",
    example: "|selected + expression|")]
  public class SelectExpressionTemplate : IPostfixTemplate<CSharpPostfixTemplateContext>
  {
    public PostfixTemplateInfo TryCreateInfo(CSharpPostfixTemplateContext context)
    {
      if (context.IsPreciseMode) return null;

      var expressions = context.Expressions.Reverse().ToArray();
      if (expressions.Length == 0) return null;

      return new PostfixTemplateInfo("sel", expressions);
    }

    public PostfixTemplateBehavior CreateBehavior(PostfixTemplateInfo info)
    {
      return new CSharpPostfixSelectExpressionBehavior(info);
    }

    private sealed class CSharpPostfixSelectExpressionBehavior : CSharpExpressionPostfixTemplateBehavior<ICSharpExpression>
    {
      public CSharpPostfixSelectExpressionBehavior([NotNull] PostfixTemplateInfo info) : base(info) { }

      protected override ICSharpExpression CreateExpression(CSharpElementFactory factory, ICSharpExpression expression)
      {
        return expression;
      }

      protected override void AfterComplete(ITextControl textControl, ICSharpExpression expression)
      {
        var expressionRange = expression.GetDocumentRange().TextRange;
        textControl.Selection.SetRange(expressionRange);
      }
    }
  }
}