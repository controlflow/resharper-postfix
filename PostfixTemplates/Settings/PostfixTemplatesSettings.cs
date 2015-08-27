using System;
using System.Linq.Expressions;
using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.Application.Settings.Store;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.Filters;

namespace JetBrains.ReSharper.PostfixTemplates.Settings
{
  [SettingsKey(typeof(EnvironmentSettings), "Postfix templates settings")]
  public sealed class PostfixTemplatesSettings
  {
    [SettingsIndexedEntry("Template providers list disabled/enabled list")]
    public IIndexedEntry<string, bool> DisabledProviders;

    [SettingsEntry(true, "Show postfix templates in code completion")]
    public bool ShowPostfixItemsInCodeCompletion;

    [SettingsEntry(true, "Show static methods as instance members in code completion")]
    public bool ShowStaticMethodsInCodeCompletion;

    // todo: [R#] drop
    [SettingsEntry(true, "Show enumeration types helpers in code completion")]
    public bool ShowEnumHelpersInCodeCompletion;

    // todo: [R#] drop
    [SettingsEntry(true, "Alias .Count property as .Length in code completion and vice versa")]
    public bool ShowLengthCountItemsInCodeCompletion;

    [SettingsEntry(true, "Insert braces for embedded statements")]
    public bool UseBracesForEmbeddedStatements;

    [SettingsEntry(false, "Invoke parameter info from templates")]
    public bool InvokeParameterInfoFromTemplates;

    [SettingsEntry(true, "Search for occurrences in .var template")]
    public bool SearchOccurrencesFromIntroduceVarTemplates;

    [SettingsEntry(CompletionListFilterState.Off, "Postfix template code completion filter")]
    public CompletionListFilterState PostfixTemplatesCodeCompletionFilter;
  }

  public static class PostfixTemplatesSettingsAccessor
  {
    [NotNull] public static readonly Expression<Func<PostfixTemplatesSettings, IIndexedEntry<string, bool>>>
      DisabledProviders    = settings => settings.DisabledProviders;

    [NotNull] public static readonly Expression<Func<PostfixTemplatesSettings, bool>>
      ShowPostfixItems     = settings => settings.ShowPostfixItemsInCodeCompletion,
      ShowStaticMethods    = settings => settings.ShowStaticMethodsInCodeCompletion,
      ShowEnumHelpers      = settings => settings.ShowEnumHelpersInCodeCompletion,
      ShowLengthCountItems = settings => settings.ShowLengthCountItemsInCodeCompletion,
      BracesForStatements  = settings => settings.UseBracesForEmbeddedStatements,
      InvokeParameterInfo  = settings => settings.InvokeParameterInfoFromTemplates,
      SearchVarOccurrences = settings => settings.SearchOccurrencesFromIntroduceVarTemplates;

    public static readonly Expression<Func<PostfixTemplatesSettings, CompletionListFilterState>>
      PostfixTemplatesCodeCompletionFilter = settings => settings.PostfixTemplatesCodeCompletionFilter;

    public static TValue GetIndexedValue<TKey, TValue>(
      [NotNull] this IIndexedEntry<TKey, TValue> settings, [NotNull] TKey key, TValue defaultValue)
    {
      TValue value;
      if (settings.TryGet(key, out value)) return value;
      return defaultValue;
    }
  }
}