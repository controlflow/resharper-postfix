using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.PostfixTemplates.Contexts;
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
  public class SelectExpressionTemplate : IPostfixTemplate
  {
    public ILookupItem CreateItem(PostfixTemplateContext context)
    {
      if (context.IsAutoCompletion) return null;

      var expressions = context.Expressions.Reverse().ToArray();
      if (expressions.Length == 0) return null;

      return new SelectItem(expressions);
    }

    private sealed class SelectItem : ExpressionPostfixLookupItem<ICSharpExpression>
    {
      public SelectItem([NotNull] CSharpPostfixExpressionContext[] contexts)
        : base("sel", contexts) { }

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