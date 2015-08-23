using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.UI.Avalon.TreeListView;
using JetBrains.UI.Options;

namespace JetBrains.ReSharper.PostfixTemplates.Settings
{
  public sealed class PostfixTemplateViewModel : ObservableObject
  {
    [NotNull] private readonly IPostfixTemplateMetadata myMetadata;
    [NotNull] private readonly PostfixTemplateAttribute myTemplateAttribute;
    [NotNull] private readonly OptionsSettingsSmartContext mySettingsStore;
    private bool myIsChecked;

    public PostfixTemplateViewModel([NotNull] IPostfixTemplateMetadata metadata, [NotNull] OptionsSettingsSmartContext settingsStore)
    {
      myMetadata = metadata;
      mySettingsStore = settingsStore;
      myTemplateAttribute = myMetadata.Metadata;

      var settings = mySettingsStore.GetKey<PostfixTemplatesSettings>(SettingsOptimization.OptimizeDefault);
      myIsChecked = settings.DisabledProviders.GetIndexedValue(metadata.SettingsKey, !metadata.Metadata.DisabledByDefault);
    }

    [NotNull] public string TemplateName { get { return "." + myTemplateAttribute.TemplateName.ToLowerInvariant(); } }
    [NotNull] public string Description { get { return myTemplateAttribute.Description; } }
    [NotNull] public string Example { get { return myTemplateAttribute.Example; } }

    public bool IsChecked
    {
      get { return myIsChecked; }
      set
      {
        if (value == myIsChecked) return;

        myIsChecked = value;
        OnPropertyChanged("IsChecked");

        var settingsKey = myMetadata.SettingsKey;
        var disabledProviders = PostfixSettingsAccessor.DisabledProviders;

        if (myIsChecked == myTemplateAttribute.DisabledByDefault)
          mySettingsStore.SetIndexedValue(disabledProviders, settingsKey, myIsChecked);
        else
          mySettingsStore.RemoveIndexedValue(disabledProviders, settingsKey);
      }
    }
  }
}