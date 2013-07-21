using System.Collections.Generic;
using System.ComponentModel;
using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.DataFlow;
using JetBrains.UI.Avalon.TreeListView;
using JetBrains.UI.Options;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.Settings
{
  public sealed class PostfixOptionsViewModel : ObservableObject
  {
    public PostfixOptionsViewModel(
      [NotNull] Lifetime lifetime,
      [NotNull] OptionsSettingsSmartContext store,
      [NotNull] PostfixTemplatesManager templatesManager)
    {
      Templates = new List<PostfixTemplateViewModel>();
      UseBraces = new Property<bool>(lifetime, "UseBraces");

      store.SetBinding(lifetime, PostfixCompletionSettingsAccessor.UseBracesForEmbeddedStatements, UseBraces);

      var settings = store.GetKey<PostfixCompletionSettings>(SettingsOptimization.OptimizeDefault);
      settings.DisabledProviders.SnapshotAndFreeze();

      PropertyChangedEventHandler handler = (_, args) =>
      {
        if (args.PropertyName != "IsChecked") return;

        var viewModel = ((PostfixTemplateViewModel) _);
        store.SetIndexedValue(
          PostfixCompletionSettingsAccessor.DisabledProviders,
          viewModel.SettingsKey, viewModel.IsChecked);
      };

      foreach (var providerInfo in templatesManager.TemplateProvidersInfos)
      {
        var metadata = providerInfo.Metadata;
        bool isEnabled = (!settings.DisabledProviders.TryGet(providerInfo.SettingsKey, out isEnabled)
                       && !metadata.DisabledByDefault) || isEnabled;

        var itemViewModel = new PostfixTemplateViewModel(
          name: string.Join("/", metadata.TemplateNames),
          description: metadata.Description,
          settingsKey: providerInfo.SettingsKey,
          isChecked: isEnabled);

        itemViewModel.PropertyChanged += handler;
        Templates.Add(itemViewModel);
      }
    }

    [NotNull] public List<PostfixTemplateViewModel> Templates { get; private set; }
    [NotNull] public IProperty<bool> UseBraces { get; private set; }
  }
}