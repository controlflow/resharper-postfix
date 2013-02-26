using JetBrains.Application.Settings;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.Settings
{
  [SettingsKey(typeof(EnvironmentSettings), "Postfix completion settings")]
  public class PostfixCompletionSettings
  {
    [SettingsEntry(true, "Foo")]
    public bool IsEnabled;
  }
}