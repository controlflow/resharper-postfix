using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.LiveTemplates;
using JetBrains.ReSharper.Feature.Services.Lookup;

namespace JetBrains.ReSharper.PostfixTemplates.Templates.CSharp
{
  [PostfixTemplate(
    templateName: "tryparse",
    description: "Parses string as value of some type",
    example: "int.TryParse(expr, out value)")]
  public class TryParseStringTemplate : ParseStringTemplateBase
  {
    public TryParseStringTemplate(
      [NotNull] LiveTemplatesManager liveTemplatesManager, [NotNull] LookupItemsOwnerFactory lookupItemsOwnerFactory)
      : base(liveTemplatesManager, lookupItemsOwnerFactory) { }

    public override string TemplateName { get { return "tryParse"; } }
    public override bool IsTryParse { get { return true; } }
  }
}