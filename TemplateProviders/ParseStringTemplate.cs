using System.Collections.Generic;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.TemplateProviders
{
  [PostfixTemplateProvider(
    templateName: "parse",
    description: "Parses string as value of some type",
    example: "int.Parse(expr)")]
  public class ParseStringTemplate : ParseStringTemplateProviderBase, IPostfixTemplate
  {
    public void CreateItems(
      PostfixTemplateAcceptanceContext context, ICollection<ILookupItem> consumer)
    {
      foreach (var exprContext in context.Expressions)
      {
        var type = exprContext.Type;
        if (type.IsResolved && type.IsString())
        {
          consumer.Add(new LookupItem("parse", exprContext, context.LookupItemsOwner, false));
          break;
        }
      }
    }
  }
}