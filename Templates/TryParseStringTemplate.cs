using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.PostfixTemplates.Templates
{
  [PostfixTemplate(
    templateName: "tryparse",
    description: "Parses string as value of some type",
    example: "int.TryParse(expr, out value)")]
  public class TryParseStringTemplate : ParseStringTemplateBase, IPostfixTemplate
  {
    public ILookupItem CreateItem(PostfixTemplateContext context)
    {
      foreach (var expressionContext in context.Expressions)
      {
        var expressionType = expressionContext.Type;
        if (expressionType.IsResolved && expressionType.IsString())
        {
          var lookupItemsOwner = context.ExecutionContext.LookupItemsOwner;
          return new ParseItem("tryParse", expressionContext, lookupItemsOwner, isTryParse: true);
        }
      }

      return null;
    }
  }
}