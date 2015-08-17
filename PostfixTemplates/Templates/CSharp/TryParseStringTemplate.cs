using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.PostfixTemplates.Contexts;
using JetBrains.ReSharper.PostfixTemplates.Contexts.CSharp;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.PostfixTemplates.Templates.CSharp
{
  [PostfixTemplate(
    templateName: "tryparse",
    description: "Parses string as value of some type",
    example: "int.TryParse(expr, out value)")]
  public class TryParseStringTemplate : ParseStringTemplateBase, IPostfixTemplate<CSharpPostfixTemplateContext>
  {
    public ILookupItem CreateItem(CSharpPostfixTemplateContext context)
    {
      foreach (var expressionContext in context.Expressions)
      {
        var expressionType = expressionContext.Type;
        if (expressionType.IsResolved && expressionType.IsString())
        {
          return new ParseItem("tryParse", expressionContext, isTryParse: true);
        }
      }

      return null;
    }
  }
}