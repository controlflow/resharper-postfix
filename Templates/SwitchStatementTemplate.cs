using JetBrains.Annotations;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.Templates
{
  // TODO: make it work over everything in force mode (do not check type)

  [PostfixTemplate(
    templateName: "switch",
    description: "Produces switch over integral/string type",
    example: "switch (expr)")]
  public class SwitchStatementTemplate : IPostfixTemplate
  {
    public ILookupItem CreateItems(PostfixTemplateContext context)
    {
      foreach (var expressionContext in context.Expressions)
      {
        if (!expressionContext.CanBeStatement) continue;

        var type = expressionContext.Type;
        if (!type.IsResolved) continue;

        if (type.IsPredefinedIntegral() || type.IsEnumType())
        {
          return new LookupItem(expressionContext);
        }
      }

      return null;
    }

    private sealed class LookupItem : StatementPostfixLookupItem<ISwitchStatement>
    {
      public LookupItem([NotNull] PrefixExpressionContext context) : base("switch", context) { }

      // switch statement can't be without braces
      protected override ISwitchStatement CreateStatement(
        CSharpElementFactory factory, ICSharpExpression expression)
      {
        var template = "switch($0)" + RequiredBracesTemplate;
        return (ISwitchStatement) factory.CreateStatement(template, expression);
      }
    }
  }
}