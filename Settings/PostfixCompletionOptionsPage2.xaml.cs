using JetBrains.ReSharper.Feature.Services.Resources;
using JetBrains.ReSharper.Features.Intellisense.Options;
using JetBrains.UI.CrossFramework;
using JetBrains.UI.Options;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.Settings
{
  [OptionsPage(PID, "Postfix completion",
    typeof(ServicesThemedIcons.SurroundTemplate),
    ParentId = IntelliSensePage.PID)]
  public sealed partial class PostfixCompletionOptionsPage2 : IOptionsPage
  {
    public const string PID = "PostfixCompletion2";

    public PostfixCompletionOptionsPage2()
    {
      Control = this;
      InitializeComponent();
    }

    public EitherControl Control { get; private set; }
    public string Id { get { return PID; } }

    public bool OnOk()
    {
      return true;
    }

    public bool ValidatePage()
    {
      return true;
    }
  }
}