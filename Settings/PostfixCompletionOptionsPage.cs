using System.Windows.Forms;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.DataFlow;
using JetBrains.ReSharper.Feature.Services.Resources;
using JetBrains.ReSharper.Features.Intellisense.Options;
using JetBrains.UI.CommonControls.Fonts;
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

    public PostfixCompletionOptionsPage([NotNull] Lifetime lifetime,
      [NotNull] OptionsSettingsSmartContext store, [NotNull] FontsManager fontsManager, [NotNull] Shell shell)
      : base(lifetime, PID, fontsManager)
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
        Width = 460,
        Height = 300,
        Sorting = SortOrder.Ascending,
      };

      listView.Columns.Add("Shortcut").Width = 100;
      listView.Columns.Add("Description").Width = 350;
      listView.ItemChecked += OnItemChecked;

      foreach (var provider in shell.GetComponents<IPostfixTemplateProvider>())
      {
        var providerType = provider.GetType();
        var attributes = (PostfixTemplateProviderAttribute[])
          providerType.GetCustomAttributes(typeof(PostfixTemplateProviderAttribute), false);

        if (attributes.Length == 1)
        {
          var attribute = attributes[0];
          var providerKey = providerType.FullName;

          bool isEnabled;
          isEnabled = !disabledProviders.TryGet(providerKey, out isEnabled) || isEnabled;

          var item = new ListViewItem(new[] {attribute.TemplateName, attribute.Description});
          item.Checked = isEnabled;
          item.Tag = providerKey;

          listView.Items.Add(item);
        }
      }

      Controls.Add(listView);
    }

    private void OnItemChecked(object sender, ItemCheckedEventArgs args)
    {
      var providerKey = (string) args.Item.Tag;
      var isEnabled = args.Item.Checked;
      if (isEnabled)
        myStore.RemoveIndexedValue((PostfixCompletionSettings x) => x.DisabledProviders, providerKey);
      else
        myStore.SetIndexedValue((PostfixCompletionSettings x) => x.DisabledProviders, providerKey, false);
    }
  }
}
