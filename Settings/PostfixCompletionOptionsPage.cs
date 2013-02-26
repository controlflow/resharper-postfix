using System.Windows.Forms;
using JetBrains.Annotations;
using JetBrains.DataFlow;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Resources;
using JetBrains.ReSharper.Features.Intellisense.Options;
using JetBrains.UI.CommonControls.Fonts;
using JetBrains.UI.Options;
using JetBrains.UI.Options.Helpers;
using JetBrains.Util.Lazy;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.Settings
{
  [OptionsPage(PID, "Postfix completion", typeof(ServicesThemedIcons.SurroundTemplate), ParentId = IntelliSensePage.PID)]
  public sealed class PostfixCompletionOptionsPage : AStackPanelOptionsPage3
  {
    public const string PID = "PostfixCompletion";

    //private readonly IProperty<bool> myIsEnabled = new Property<bool>("IsEnabled");

    public PostfixCompletionOptionsPage([NotNull] Lifetime lifetime, [NotNull] OptionsSettingsSmartContext smartContext,
      [NotNull] FontsManager fontsManager, [NotNull] Lazy<ISolution> solution)
      : base(lifetime, PID, fontsManager)
    {
      if (solution.Value == null) return;

      var listView = new ListView();
      listView.View = View.Details;
      listView.CheckBoxes = true;

      //listView.Anchor = AnchorStyles.Top;
      //listView.Dock = DockStyle.Top;


      //listView.Sorting = SortOrder.Ascending;

      listView.Columns.Add("Name");
      listView.Columns.Add("Description");

      var providers = solution.Value.GetComponents<IPostfixTemplateProvider>();
      foreach (var provider in providers)
      {
        var attributes = (PostfixTemplateProviderAttribute[])
          provider.GetType().GetCustomAttributes(typeof(PostfixTemplateProviderAttribute), false);

        if (attributes.Length == 1)
        {
          var templateName = attributes[0].TemplateName;
          listView.Items.Add(new ListViewItem(new[] { templateName, templateName + " description" }));
        }
      }

      Controls.Add(listView);

      //smartContext.SetBinding(lifetime, (SurroundCompletionSettings x) => x.IsEnabled, myIsEnabled);



      //var checkBoxEnabled = new Controls.CheckBox {
      //  Text = "Enabled"
      //};
      //
      //Controls.Add(checkBoxEnabled);
      //
      //new PropertyBinding<bool, bool>(lifetime, myIsEnabled, checkBoxEnabled.Checked, DataFlowDirection.BothWays);
    }
  }
}
