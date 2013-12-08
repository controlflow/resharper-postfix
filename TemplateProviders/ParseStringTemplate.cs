using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.TemplateProviders
{
  [PostfixTemplate(
    templateName: "parse",
    description: "Parses string as value of some type",
    example: "int.Parse(expr)")]
  public class ParseStringTemplate : ParseStringTemplateBase, IPostfixTemplate {
    public ILookupItem CreateItems(PostfixTemplateContext context) {
      foreach (var exprContext in context.Expressions) {
        var type = exprContext.Type;
        if (type.IsResolved && type.IsString()) {
          return new LookupItem("parse", exprContext, context.LookupItemsOwner, false);
        }
      }

      return null;
    }
  }
}