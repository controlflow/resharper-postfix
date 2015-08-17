using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.Application.Settings;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.PostfixTemplates.Contexts;
using JetBrains.ReSharper.PostfixTemplates.Settings;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.PostfixTemplates
{
  [ShellComponent]
  public class PostfixTemplatesManager
  {
    [NotNull] private readonly LanguageManager myLanguageManager;
    [NotNull] private readonly IList<TemplateProviderInfo> myTemplateProvidersInfos;

    public PostfixTemplatesManager([NotNull] IEnumerable<IPostfixTemplate> providers, [NotNull] LanguageManager languageManager)
    {
      myLanguageManager = languageManager;




      var infos = new List<TemplateProviderInfo>();
      foreach (var provider in providers)
      {
        var providerType = provider.GetType();
        var attributes = (PostfixTemplateAttribute[])
          providerType.GetCustomAttributes(typeof(PostfixTemplateAttribute), inherit: false);

        if (attributes.Length == 1)
        {
          var info = new TemplateProviderInfo(provider, attributes[0], providerType.FullName);
          infos.Add(info);
        }
      }

      myTemplateProvidersInfos = infos.AsReadOnly();
    }

    public sealed class TemplateProviderInfo
    {
      public TemplateProviderInfo([NotNull] IPostfixTemplate provider,
                                  [NotNull] PostfixTemplateAttribute metadata,
                                  [NotNull] string providerKey)
      {
        Provider = provider;
        Metadata = metadata;
        SettingsKey = providerKey;
      }

      [NotNull] public IPostfixTemplate Provider { get; private set; }
      [NotNull] public PostfixTemplateAttribute Metadata { get; private set; }
      [NotNull] public string SettingsKey { get; private set; }
    }

    [NotNull] public IList<TemplateProviderInfo> TemplateProvidersInfos
    {
      get { return myTemplateProvidersInfos; }
    }

    [NotNull]
    public IList<ILookupItem> CollectItems([NotNull] PostfixTemplateContext context, [CanBeNull] string templateName = null)
    {
      var store = context.Reference.GetSettingsStore();
      var settings = store.GetKey<PostfixTemplatesSettings>(SettingsOptimization.OptimizeDefault);
      settings.DisabledProviders.SnapshotAndFreeze();

      // todo: restore this?

      //var innerExpression = context.InnerExpression; // shit happens
      //if (innerExpression != null && innerExpression.ReferencedElement is INamespace)
      //{
      //  return EmptyList<ILookupItem>.InstanceList;
      //}

      var lookupItems = new List<ILookupItem>();
      foreach (var info in myTemplateProvidersInfos)
      {
        // check disabled providers
        {
          bool isEnabled;
          if (!settings.DisabledProviders.TryGet(info.SettingsKey, out isEnabled))
          {
            isEnabled = !info.Metadata.DisabledByDefault;
          }

          if (!isEnabled) continue;
        }

        if (templateName != null)
        {
          var name = info.Metadata.TemplateName;
          if (!string.Equals(templateName, name, StringComparison.Ordinal)) continue;
        }

        var lookupItem = info.Provider.CreateItem(context);
        if (lookupItem != null)
        {
          lookupItems.Add(lookupItem);
        }
      }

      return lookupItems;
    }

    [CanBeNull]
    public PostfixTemplateContext IsAvailable([CanBeNull] ITreeNode position, [NotNull] PostfixExecutionContext executionContext)
    {
      if (position == null) return null;

      var contextFactory = myLanguageManager.TryGetService<IPostfixTemplateContextFactory>(position.Language);
      if (contextFactory == null) return null;

      return contextFactory.TryCreate(position, executionContext);
    }
  }
}