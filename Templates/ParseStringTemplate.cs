using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.Templates
{
  [PostfixTemplate(
    templateName: "parse",
    description: "Parses string as value of some type",
    example: "int.Parse(expr)")]
  public class ParseStringTemplate : ParseStringTemplateBase, IPostfixTemplate
  {
    public ILookupItem CreateItems(PostfixTemplateContext context)
    {
      foreach (var expressionContext in context.Expressions)
      {
        var expressionType = expressionContext.Type;
        if (expressionType.IsResolved && expressionType.IsString())
        {
          return new LookupItem("parse", expressionContext, context.LookupItemsOwner, false);
        }
      }

      return null;
    }
  }
}