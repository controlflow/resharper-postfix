using JetBrains.Application.Settings;
using JetBrains.Application.Settings.Store;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.Settings
{
  [SettingsKey(typeof(EnvironmentSettings), "Postfix completion settings")]
  public class PostfixCompletionSettings
  {
    [SettingsIndexedEntry("Template providers list disabled/enabled list")]
    public IIndexedEntry<string, bool> DisabledProviders;

    //[SettingsEntry(true, "Insert braces for embedded statements")]
    //public bool UseBracesForEmbeddedStatements;
  }
}