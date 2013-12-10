using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.LiveTemplates;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.Templates
{
  [PostfixTemplate(
    templateName: "tryparse",
    description: "Parses string as value of some type",
    example: "int.TryParse(expr, out value)")]
  public class TryParseStringTemplate : ParseStringTemplateBase, IPostfixTemplate
  {
    [NotNull] private readonly LiveTemplatesManager myTemplatesManager;
    [NotNull] private readonly IShellLocks myShellLocks;

    public TryParseStringTemplate(
      [NotNull] LiveTemplatesManager templatesManager, [NotNull] IShellLocks shellLocks)
    {
      myTemplatesManager = templatesManager;
      myShellLocks = shellLocks;
    }

    public ILookupItem CreateItems(PostfixTemplateContext context)
    {
      foreach (var expressionContext in context.Expressions)
      {
        var expressionType = expressionContext.Type;
        if (expressionType.IsResolved && expressionType.IsString())
        {
          return new ParseLookupItem(
            "tryParse", expressionContext, myTemplatesManager,
            myShellLocks, context.LookupItemsOwner, isTryParse: true);
        }
      }

      return null;
    }
  }
}