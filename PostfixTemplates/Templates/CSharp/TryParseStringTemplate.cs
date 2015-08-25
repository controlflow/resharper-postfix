namespace JetBrains.ReSharper.PostfixTemplates.Templates.CSharp
{
  [PostfixTemplate(
    templateName: "tryparse",
    description: "Parses string as value of some type",
    example: "int.TryParse(expr, out value)")]
  public class TryParseStringTemplate : ParseStringTemplateBase
  {
    public override string TemplateName { get { return "tryParse"; } }
    public override bool IsTryParse { get { return true; } }
  }
}