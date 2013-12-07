using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.TemplateProviders
{
  // TODO: make it work over everything in force mode (do not check type)
  
  [PostfixTemplateProvider(
    templateName: "switch",
    description: "Produces switch over integral/string type",
    example: "switch (expr)")]
  public class SwitchStatementTemplate : IPostfixTemplate
  {
    public void CreateItems(PostfixTemplateAcceptanceContext context, ICollection<ILookupItem> consumer)
    {
      foreach (var exprContext in context.Expressions)
      {
        if (!exprContext.CanBeStatement) continue;

        var type = exprContext.Type;
        if (!type.IsResolved) continue;

        if (type.IsPredefinedIntegral() || type.IsEnumType())
        {
          consumer.Add(new LookupItem(exprContext));
          break;
        }
      }
    }

    private sealed class LookupItem : KeywordStatementPostfixLookupItem<ISwitchStatement>
    {
      public LookupItem([NotNull] PrefixExpressionContext context)
        : base("switch", context) { }

      protected override string Template
      {
        get { return "switch(expr)"; }
      }

      // switch statement can't be without braces
      protected override ISwitchStatement CreateStatement(CSharpElementFactory factory)
      {
        var template = Template + "{" + CaretMarker + ";}";
        return (ISwitchStatement) factory.CreateStatement(template);
      }

      protected override void PlaceExpression(
        ISwitchStatement statement, ICSharpExpression expression, CSharpElementFactory factory)
      {
        statement.Condition.ReplaceBy(expression);
      }
    }
  }
}
