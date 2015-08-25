namespace JetBrains.ReSharper.PostfixTemplates.Templates.CSharp
{
  [PostfixTemplate(
    templateName: "parse",
    description: "Parses string as value of some type",
    example: "int.Parse(expr)")]
  public class ParseStringTemplate : ParseStringTemplateBase
  {
    public override string TemplateName { get { return "parse"; } }
    public override bool IsTryParse { get { return false; } }
  }
}