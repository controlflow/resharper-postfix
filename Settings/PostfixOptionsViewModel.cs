using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.DataFlow;
using JetBrains.UI.Extensions.Commands;
using JetBrains.UI.Options;

namespace JetBrains.ReSharper.PostfixTemplates.Settings
{
  public sealed class PostfixOptionsViewModel
  {
    [NotNull] private readonly OptionsSettingsSmartContext mySettingsStore;
    [NotNull] private readonly PostfixTemplatesManager myTemplatesManager;

    public PostfixOptionsViewModel([NotNull] Lifetime lifetime,
                                   [NotNull] OptionsSettingsSmartContext settings,
                                   [NotNull] PostfixTemplatesManager templatesManager)
    {
      mySettingsStore = settings;
      myTemplatesManager = templatesManager;
      Templates = new ObservableCollection<PostfixTemplateViewModel>();
      ShowPostfixTemplatesInCodeCompletion = new Property<bool>(lifetime, "ShowPostfixTemplatesInCodeCompletion");
      ShowStaticMembersInCodeCompletion = new Property<bool>(lifetime, "ShowStaticMethodsInCodeCompletion");
      ShowEnumHelpersInCodeCompletion = new Property<bool>(lifetime, "ShowEnumHelpersInCodeCompletion");
      UseBracesForEmbeddedStatements = new Property<bool>(lifetime, "UseBracesForEmbeddedStatements");
      Reset = new DelegateCommand(ResetExecute);

      settings.SetBinding(lifetime, PostfixSettingsAccessor.ShowPostfixItems, ShowPostfixTemplatesInCodeCompletion);
      settings.SetBinding(lifetime, PostfixSettingsAccessor.ShowStaticMethods, ShowStaticMembersInCodeCompletion);
      settings.SetBinding(lifetime, PostfixSettingsAccessor.ShowEnumHelpers, ShowEnumHelpersInCodeCompletion);
      settings.SetBinding(lifetime, PostfixSettingsAccessor.BracesForStatements, UseBracesForEmbeddedStatements);
      settings.SetBinding(lifetime, PostfixSettingsAccessor.InvokeParameterInfo, InvokeParameterInfoFromTemplates);

      FillTemplates();
    }

    [NotNull] public ObservableCollection<PostfixTemplateViewModel> Templates { get; private set; }
    [NotNull] public IProperty<bool> ShowPostfixTemplatesInCodeCompletion { get; private set; }
    [NotNull] public IProperty<bool> ShowStaticMembersInCodeCompletion { get; private set; }
    [NotNull] public IProperty<bool> ShowEnumHelpersInCodeCompletion { get; private set; }
    [NotNull] public IProperty<bool> UseBracesForEmbeddedStatements { get; private set; }
    [NotNull] public IProperty<bool> InvokeParameterInfoFromTemplates { get; private set; }
    [NotNull] public ICommand Reset { get; private set; }

    private void FillTemplates()
    {
      var settings = mySettingsStore.GetKey<PostfixTemplatesSettings>(SettingsOptimization.OptimizeDefault);
      settings.DisabledProviders.SnapshotAndFreeze();

      PropertyChangedEventHandler handler = (sender, args) =>
      {
        if (args.PropertyName == "IsChecked")
        {
          var viewModel = (PostfixTemplateViewModel) sender;
          mySettingsStore.SetIndexedValue(
            PostfixSettingsAccessor.DisabledProviders,
            viewModel.SettingsKey, viewModel.IsChecked);
        }
      };

      var infos = myTemplatesManager.TemplateProvidersInfos;
      foreach (var providerInfo in infos.OrderBy(providerInfo => providerInfo.Metadata.TemplateName))
      {
        var metadata = providerInfo.Metadata;
        bool isEnabled = (!settings.DisabledProviders.TryGet(providerInfo.SettingsKey, out isEnabled)
                          && !metadata.DisabledByDefault) || isEnabled;

        var itemViewModel = new PostfixTemplateViewModel(
          name: metadata.TemplateName, description: metadata.Description,
          example: metadata.Example, settingsKey: providerInfo.SettingsKey,
          isChecked: isEnabled);

        itemViewModel.PropertyChanged += handler;
        Templates.Add(itemViewModel);
      }
    }

    private void ResetExecute()
    {
      var settings = mySettingsStore.GetKey<PostfixTemplatesSettings>(SettingsOptimization.OptimizeDefault);
      settings.DisabledProviders.SnapshotAndFreeze();

      foreach (var provider in settings.DisabledProviders.EnumIndexedValues())
      {
        mySettingsStore.RemoveIndexedValue(PostfixSettingsAccessor.DisabledProviders, provider.Key);
      }

      Templates.Clear();
      FillTemplates();
    }
  }
}