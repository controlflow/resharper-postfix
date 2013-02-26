using JetBrains.Application.Settings;
using JetBrains.Application.Settings.Store;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.Settings
{
  [SettingsKey(typeof(EnvironmentSettings), "Postfix completion settings")]
  public class PostfixCompletionSettings
  {
    [SettingsEntry(true, "Foo")]
    public bool IsEnabled;

    //public IIndexedEntry<string, bool> DisabledAction

  }
}