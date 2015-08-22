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

      bool isEnabled;
      var isConfigured = settings.DisabledProviders.TryGet(metadata.SettingsKey, out isEnabled);
      myIsChecked = (!isConfigured && !metadata.Metadata.DisabledByDefault) || isEnabled;
    }

    [NotNull] public string Name { get { return "." + myTemplateAttribute.TemplateName.ToLowerInvariant(); } }
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

        if (myIsChecked == myTemplateAttribute.DisabledByDefault)
          mySettingsStore.SetIndexedValue(PostfixSettingsAccessor.DisabledProviders, myMetadata.SettingsKey, myIsChecked);
        else
          mySettingsStore.RemoveIndexedValue(PostfixSettingsAccessor.DisabledProviders, myMetadata.SettingsKey);
      }
    }
  }
}