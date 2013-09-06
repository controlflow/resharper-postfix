using JetBrains.Annotations;
using JetBrains.UI.Avalon.TreeListView;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.Settings
{
  public sealed class PostfixTemplateViewModel : ObservableObject
  {
    private bool myIsChecked;

    public PostfixTemplateViewModel([NotNull] string name,
      [NotNull] string description, [NotNull] string example,
      [NotNull] string settingsKey, bool isChecked)
    {
      Name = name;
      Description = description;
      Example = example;
      SettingsKey = settingsKey;
      IsChecked = isChecked;
    }

    [NotNull] public string Name { get; private set; }
    [NotNull] public string Description { get; private set; }
    [NotNull] public string Example { get; private set; }

    [NotNull] public string SettingsKey { get; private set; }

    public bool IsChecked
    {
      get { return myIsChecked; }
      set
      {
        if (value == myIsChecked) return;
        myIsChecked = value;
        OnPropertyChanged("IsChecked");
      }
    }
  }
}