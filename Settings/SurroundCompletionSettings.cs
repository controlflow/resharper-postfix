using JetBrains.Application.Settings;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.Settings
{
  [SettingsKey(typeof(EnvironmentSettings), "Postfix completion settings")]
  public class SurroundCompletionSettings
  {
    [SettingsEntry(true, "Foo")]
    public bool IsEnabled;
  }
}