using System;
using System.Linq.Expressions;
using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.Application.Settings.Store;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.Settings
{
  [SettingsKey(typeof(EnvironmentSettings), "Postfix completion settings")]
  public sealed class PostfixCompletionSettings
  {
    [SettingsIndexedEntry("Template providers list disabled/enabled list")]
    public IIndexedEntry<string, bool> DisabledProviders;

    [SettingsIndexedEntry("Template providers shortcuts list")]
    public IndexedEntry<string, string> ProviderShortcutNames;

    [SettingsIndexedEntry("Template providers usage count")]
    public IndexedEntry<string, int> ProvidersUsageStatistics;
      
    [SettingsEntry(true, "Insert braces for embedded statements")]
    public bool UseBracesForEmbeddedStatements;

    [SettingsEntry(true, "Show static methods as instance members in code completion")]
    public bool ShowStaticMethodsInCodeCompletion;

    [SettingsEntry(true, "Show enumeration types helpers in code completion")]
    public bool ShowEnumHelpersInCodeCompletion;
  }

  public static class PostfixSettingsAccessor
  {
    [NotNull] public static readonly Expression<Func<PostfixCompletionSettings, IIndexedEntry<string, bool>>>
      DisabledProviders = x => x.DisabledProviders;
    [NotNull] public static readonly Expression<Func<PostfixCompletionSettings, bool>>
      UseBracesForEmbeddedStatements = x => x.UseBracesForEmbeddedStatements,
      ShowStaticMethodsInCodeCompletion = x => x.ShowStaticMethodsInCodeCompletion,
      ShowEnumHelpersInCodeCompletion = x => x.ShowEnumHelpersInCodeCompletion;
  }
}