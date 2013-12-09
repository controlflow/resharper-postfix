using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.TemplateProviders
{
  [PostfixTemplate(
    templateName: "tryparse",
    description: "Parses string as value of some type",
    example: "int.TryParse(expr, out value)")]
  public class TryParseStringTemplate : ParseStringTemplateBase, IPostfixTemplate {
    public ILookupItem CreateItems(PostfixTemplateContext context) {
      foreach (var expressionContext in context.Expressions) {
        var type = expressionContext.Type;
        if (type.IsResolved && type.IsString()) {
          return new LookupItem("tryParse", expressionContext, context.LookupItemsOwner, true);
        }
      }

      return null;
    }
  }
}