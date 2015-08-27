using JetBrains.ActionManagement;
using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.BaseInfrastructure;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.Filters;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.Filters.CLRFilters;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Lookup;
using JetBrains.ReSharper.Feature.Services.Resources;
using JetBrains.ReSharper.PostfixTemplates.LookupItems;
using JetBrains.ReSharper.PostfixTemplates.Settings;
using JetBrains.ReSharper.Psi;
using JetBrains.UI.ActionsRevised;
using JetBrains.UI.Icons;

namespace JetBrains.ReSharper.PostfixTemplates.CodeCompletion
{
  [Language(typeof(KnownLanguage))]
  public class PostfixTemplatesFilter : CompletionListFilterBase<ISpecificCodeCompletionContext, IAspectLookupItem<PostfixTemplateInfo>>
  {
    public PostfixTemplatesFilter([NotNull] ISettingsStore store) : base(store) { }

    protected override SettingsScalarEntry GetSettingsEntryInternal(ISettingsStore store)
    {
      // todo: [R#] look at CSharpFilterStateSettingsKey
      return store.Schema.GetScalarEntry(PostfixTemplatesSettingsAccessor.PostfixTemplatesCodeCompletionFilter);
    }

    public override IconId GetImage(ISolution solution)
    {
      return ServicesThemedIcons.LiveTemplate.Id;
    }

    public override string Text { get { return "Postfix templates"; } }

    public override string ActionId { get { return PostfixFilterIds.Postfix; } }

    public override double Order
    {
      // todo: [R#] introduce order
      get { return CLRFiltersOrder.Keywords + 1; }
    }
  }

  // todo: [R#] merge with FilterIds
  public static class PostfixFilterIds
  {
    public const string Postfix = "IntelliSense_FilterPostfix";
    public const string PostfixInvert = "IntelliSense_FilterPostfix_Invert";
  }

  [Action(
    actionId: PostfixFilterIds.Postfix,
    text: "Filter Postfix Templates",
    IdeaShortcuts = new[] { "Alt+O" },
    VsShortcuts = new[] { "Alt+O" },
    ShortcutScope = ShortcutScope.TextEditor,
    Id = 534645)]
  public class FilterAggregateAction : FilterActionBase
  {
    public override string FilterId
    {
      get { return PostfixFilterIds.Postfix; }
    }
  }

  [Action(
    actionId: PostfixFilterIds.PostfixInvert,
    text: "Filter Postfix Templates Invert",
    IdeaShortcuts = new[] { "Alt+I L", "Alt+I Alt+O" },
    VsShortcuts = new[] { "Alt+I L", "Alt+I Alt+O" },
    ShortcutScope = ShortcutScope.TextEditor,
    Id = 534646)]
  public class FilterAggregateInvertAction : FilterActionBase
  {
    public override string FilterId
    {
      get { return PostfixFilterIds.PostfixInvert; }
    }

    public override bool Reverse { get { return true; } }
  }
}