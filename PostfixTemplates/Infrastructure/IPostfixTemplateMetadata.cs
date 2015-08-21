using JetBrains.Annotations;

namespace JetBrains.ReSharper.PostfixTemplates
{
  public interface IPostfixTemplateMetadata
  {
    [NotNull] PostfixTemplateAttribute Metadata { get; }
    [NotNull] string SettingsKey { get; }
  }
}