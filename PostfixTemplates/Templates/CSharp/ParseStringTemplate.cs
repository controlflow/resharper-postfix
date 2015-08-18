using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.LiveTemplates;
using JetBrains.ReSharper.Feature.Services.Lookup;

namespace JetBrains.ReSharper.PostfixTemplates.Templates.CSharp
{
  [PostfixTemplate(
    templateName: "parse",
    description: "Parses string as value of some type",
    example: "int.Parse(expr)")]
  public class ParseStringTemplate : ParseStringTemplateBase
  {
    public ParseStringTemplate(
      [NotNull] LiveTemplatesManager liveTemplatesManager, [NotNull] LookupItemsOwnerFactory lookupItemsOwnerFactory)
      : base(liveTemplatesManager, lookupItemsOwnerFactory) { }

    public override string TemplateName { get { return "parse"; } }
    public override bool IsTryParse { get { return false; } }
  }
}