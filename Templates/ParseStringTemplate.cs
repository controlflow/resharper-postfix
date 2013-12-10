using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.LiveTemplates;
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
    [NotNull] private readonly LiveTemplatesManager myTemplatesManager;
    [NotNull] private readonly IShellLocks myShellLocks;

    public ParseStringTemplate(
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
          var lookupItemsOwner = context.ExecutionContext.LookupItemsOwner;
          return new ParseLookupItem(
            "parse", expressionContext, myTemplatesManager,
            myShellLocks, lookupItemsOwner, isTryParse: false);
        }
      }

      return null;
    }
  }
}