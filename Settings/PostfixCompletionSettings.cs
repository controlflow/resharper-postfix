using JetBrains.Application.Settings;
using JetBrains.Application.Settings.Store;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.Settings
{
  [SettingsKey(typeof(EnvironmentSettings), "Postfix completion settings")]
  public class PostfixCompletionSettings
  {
    [SettingsIndexedEntry("Template providers list disabled/enabled list. Every provider is enabled by default.")]
    public IIndexedEntry<string, bool> DisabledProviders;
  }
}