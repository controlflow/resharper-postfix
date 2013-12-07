using System.Collections.Generic;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.TemplateProviders
{
  [PostfixTemplateProvider(
    templateName: "tryparse",
    description: "Parses string as value of some type",
    example: "int.TryParse(expr, out value)")]
  public class TryParseStringTemplate : ParseStringTemplateProviderBase, IPostfixTemplate
  {
    public void CreateItems(
      PostfixTemplateAcceptanceContext context, ICollection<ILookupItem> consumer)
    {
      foreach (var exprContext in context.Expressions)
      {
        var type = exprContext.Type;
        if (type.IsResolved && type.IsString())
        {
          consumer.Add(new LookupItem("tryParse", exprContext, context.LookupItemsOwner, true));
          break;
        }
      }
    }
  }
}