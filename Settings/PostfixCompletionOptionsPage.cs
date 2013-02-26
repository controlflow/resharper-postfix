using System.Windows.Forms;
using JetBrains.Annotations;
using JetBrains.Application;
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

    public PostfixCompletionOptionsPage(
      [NotNull] Lifetime lifetime, [NotNull] OptionsSettingsSmartContext smartContext,
      [NotNull] FontsManager fontsManager, [NotNull] Shell shell)
      : base(lifetime, PID, fontsManager)
    {
      var listView = new ListView();

      listView.View = View.Details;
      listView.CheckBoxes = true;
      listView.Width = 450;
      listView.Height = 300;
      listView.Sorting = SortOrder.Ascending;

      listView.Columns.Add("Shortcut").Width = 100;
      listView.Columns.Add("Description").Width = 350;

      var providers = shell.GetComponents<IPostfixTemplateProvider>();
      foreach (var provider in providers)
      {
        var attributes = (PostfixTemplateProviderAttribute[])
          provider.GetType().GetCustomAttributes(typeof(PostfixTemplateProviderAttribute), false);
        if (attributes.Length == 1)
        {
          listView.Items.Add(new ListViewItem(new[]
          {
            attributes[0].TemplateName,
            attributes[0].Description
          }));
        }
      }

      Controls.Add(listView);
    }
  }
}
