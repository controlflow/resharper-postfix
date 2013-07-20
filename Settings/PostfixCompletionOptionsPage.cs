using System.Windows.Forms;
using JetBrains.Annotations;
using JetBrains.DataFlow;
using JetBrains.ReSharper.Feature.Services.Resources;
using JetBrains.ReSharper.Features.Intellisense.Options;
using JetBrains.UI.Options;
using JetBrains.UI.Options.Helpers;
using JetBrains.Application.Settings;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.Settings
{
  [OptionsPage(PID, "Postfix completion",
    typeof(ServicesThemedIcons.SurroundTemplate), ParentId = IntelliSensePage.PID)]
  public sealed class PostfixCompletionOptionsPage : AStackPanelOptionsPage3
  {
    public const string PID = "PostfixCompletion";

    [NotNull] private readonly OptionsSettingsSmartContext myStore;

    public PostfixCompletionOptionsPage(
      [NotNull] Lifetime lifetime, [NotNull] OptionsSettingsSmartContext store,
      [NotNull] PostfixTemplatesManager templatesManager,

#if RESHARPER8
      [NotNull] UI.Application.IUIApplication application)
      : base(lifetime, application, PID)
#else
      [NotNull] UI.CommonControls.Fonts.FontsManager fontsManager)
      : base(lifetime, PID, fontsManager)
#endif
    {
      myStore = store;

      var label = new Label {Text = "Available templates:", AutoSize = true};
      Controls.Add(label);

      var completionSettings = store.GetKey<PostfixCompletionSettings>(SettingsOptimization.OptimizeDefault);
      completionSettings.DisabledProviders.SnapshotAndFreeze();
      var disabledProviders = completionSettings.DisabledProviders;

      var listView = new ListView
      {
        View = View.Details,
        CheckBoxes = true,
        Width = 480,
        Height = 300,
        Sorting = SortOrder.Ascending,
      };

      listView.Columns.Add("Shortcut").Width = 100;
      listView.Columns.Add("Description").Width = 350;
      listView.ItemChecked += OnItemChecked;

      foreach (var info in templatesManager.TemplateProvidersInfos)
      {
        bool isEnabled;
        isEnabled = (!disabledProviders.TryGet(info.SettingsKey, out isEnabled) &&
                     !info.Metadata.DisabledByDefault) || isEnabled;

        var items = new[] {string.Join("/", info.Metadata.TemplateNames), info.Metadata.Description};
        var item = new ListViewItem(items) { Checked = isEnabled, Tag = info.SettingsKey };

        listView.Items.Add(item);
      }

      Controls.Add(listView);
    }

    private void OnItemChecked(object sender, ItemCheckedEventArgs args)
    {
      var providerKey = (string) args.Item.Tag;
      var isEnabled = args.Item.Checked;

      myStore.SetIndexedValue(PostfixCompletionSettingsAccessor.DisabledProviders, providerKey, isEnabled);
    }
  }
}
