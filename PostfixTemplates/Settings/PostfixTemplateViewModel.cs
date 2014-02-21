using JetBrains.Annotations;
using JetBrains.UI.Avalon.TreeListView;

namespace JetBrains.ReSharper.PostfixTemplates.Settings
{
  public sealed class PostfixTemplateViewModel : ObservableObject
  {
    [NotNull] private readonly PostfixTemplateAttribute myMetadata;
    private bool myIsChecked;

    public PostfixTemplateViewModel([NotNull] PostfixTemplateAttribute metadata,
                                    [NotNull] string settingsKey, bool isChecked)
    {
      myMetadata = metadata;
      SettingsKey = settingsKey;
      IsChecked = isChecked;
    }

    [NotNull] public string Name
    {
      get { return "." + myMetadata.TemplateName.ToLowerInvariant(); }
    }

    [NotNull] public string Description
    {
      get { return myMetadata.Description; }
    }

    [NotNull] public string Example
    {
      get { return myMetadata.Example; }
    }

    [NotNull] public string SettingsKey
    {
      get; private set;
    }

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

    public bool DefaultValue
    {
      get { return !myMetadata.DisabledByDefault; }
    }
  }
}