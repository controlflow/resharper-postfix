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

    [SettingsEntry(true, "Insert braces for embedded statements")]
    public bool UseBracesForEmbeddedStatements;

    [SettingsEntry(true, "Show static members in instance members code comopletion")]
    public bool ShowStaticMembersInCodeCompletion;
  }

  public static class PostfixCompletionSettingsAccessor
  {
    [NotNull] public static readonly Expression<Func<PostfixCompletionSettings, IIndexedEntry<string, bool>>> DisabledProviders = x => x.DisabledProviders;
    [NotNull] public static readonly Expression<Func<PostfixCompletionSettings, bool>> UseBracesForEmbeddedStatements = x => x.UseBracesForEmbeddedStatements;
  }
}