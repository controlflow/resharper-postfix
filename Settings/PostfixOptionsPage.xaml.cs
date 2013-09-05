using JetBrains.Annotations;
using JetBrains.DataFlow;
using JetBrains.ReSharper.Feature.Services.Resources;
using JetBrains.UI.Application.PluginSupport;
using JetBrains.UI.CrossFramework;
using JetBrains.UI.Options;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.Settings
{
  [OptionsPage(
    id: PID,
    name: "Postfix completion",
    typeofIcon: typeof(ServicesThemedIcons.SurroundTemplate),
    ParentId = PluginsPage.Pid)]
  public sealed partial class PostfixOptionsPage : IOptionsPage
  {
    public const string PID = "PostfixCompletion";

    public PostfixOptionsPage([NotNull] Lifetime lifetime,
      [NotNull] OptionsSettingsSmartContext store,
      [NotNull] PostfixTemplatesManager templatesManager)
    {
      InitializeComponent();
      DataContext = new PostfixOptionsViewModel(lifetime, store, templatesManager);
      Control = this;
    }

    public EitherControl Control { get; private set; }
    public string Id { get { return PID; } }
    public bool OnOk() { return true; }
    public bool ValidatePage() { return true; }
  }
}