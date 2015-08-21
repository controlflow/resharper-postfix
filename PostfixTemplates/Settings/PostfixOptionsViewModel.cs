using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.DataFlow;
using JetBrains.ReSharper.Psi;
using JetBrains.UI.Extensions.Commands;
using JetBrains.UI.Options;

namespace JetBrains.ReSharper.PostfixTemplates.Settings
{
  public sealed class PostfixOptionsViewModel
  {
    [NotNull] private readonly OptionsSettingsSmartContext mySettingsStore;

    public PostfixOptionsViewModel([NotNull] Lifetime lifetime,
                                   [NotNull] OptionsSettingsSmartContext settings,
                                   [NotNull] LanguageManager languageManager)
    {
      mySettingsStore = settings;

      Templates = new ObservableCollection<PostfixTemplateViewModel>();

      ShowPostfixTemplates = new Property<bool>(lifetime, "ShowPostfixTemplates");
      ShowStaticMembers = new Property<bool>(lifetime, "ShowStaticMembers");
      ShowEnumHelpers = new Property<bool>(lifetime, "ShowEnumHelpers");
      ShowLengthCountItems = new Property<bool>(lifetime, "ShowLengthCountItems");
      UseBracesForStatements = new Property<bool>(lifetime, "UseBracesForStatements");
      InvokeParameterInfo = new Property<bool>(lifetime, "InvokeParameterInfo");
      SearchVarOccurrences = new Property<bool>(lifetime, "SearchVarOccurrences");

      Reset = new DelegateCommand(ResetExecute);

      settings.SetBinding(lifetime, PostfixSettingsAccessor.ShowPostfixItems, ShowPostfixTemplates);
      settings.SetBinding(lifetime, PostfixSettingsAccessor.ShowStaticMethods, ShowStaticMembers);
      settings.SetBinding(lifetime, PostfixSettingsAccessor.ShowEnumHelpers, ShowEnumHelpers);
      settings.SetBinding(lifetime, PostfixSettingsAccessor.BracesForStatements, UseBracesForStatements);
      settings.SetBinding(lifetime, PostfixSettingsAccessor.InvokeParameterInfo, InvokeParameterInfo);
      settings.SetBinding(lifetime, PostfixSettingsAccessor.ShowLengthCountItems, ShowLengthCountItems);
      settings.SetBinding(lifetime, PostfixSettingsAccessor.SearchVarOccurrences, SearchVarOccurrences);

      FillTemplates();
    }

    // ReSharper disable once CollectionNeverQueried.Global
    [NotNull] public ObservableCollection<PostfixTemplateViewModel> Templates { get; private set; }

    [NotNull] public IProperty<bool> ShowPostfixTemplates { get; private set; }
    [NotNull] public IProperty<bool> ShowStaticMembers { get; private set; }
    [NotNull] public IProperty<bool> ShowEnumHelpers { get; private set; }
    [NotNull] public IProperty<bool> ShowLengthCountItems { get; private set; }
    [NotNull] public IProperty<bool> UseBracesForStatements { get; private set; }
    [NotNull] public IProperty<bool> InvokeParameterInfo { get; private set; }
    [NotNull] public IProperty<bool> SearchVarOccurrences { get; private set; }

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
          if (viewModel.IsChecked == viewModel.DefaultValue)
            mySettingsStore.RemoveIndexedValue(
              PostfixSettingsAccessor.DisabledProviders, viewModel.SettingsKey);
          else
            mySettingsStore.SetIndexedValue(
              PostfixSettingsAccessor.DisabledProviders, viewModel.SettingsKey, viewModel.IsChecked);
        }
      };

      var managers = LanguageManager.Instance.GetServicesFromAll<IPostfixTemplatesManager>();
      foreach (var manager in managers)
      {
        var infos = manager.AvailableTemplates;


        foreach (var providerInfo in infos.OrderBy(providerInfo => providerInfo.Metadata.TemplateName))
        {
          var metadata = providerInfo.Metadata;
          // ReSharper disable once SuggestUseVarKeywordEverywhere
          bool isEnabled = (!settings.DisabledProviders.TryGet(providerInfo.SettingsKey, out isEnabled)
                            && !metadata.DisabledByDefault) || isEnabled;

          var itemViewModel = new PostfixTemplateViewModel(
            metadata: metadata, settingsKey: providerInfo.SettingsKey, isChecked: isEnabled);

          itemViewModel.PropertyChanged += handler;
          Templates.Add(itemViewModel);
        }
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