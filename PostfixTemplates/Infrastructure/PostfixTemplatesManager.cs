using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.PostfixTemplates.Contexts;
using JetBrains.ReSharper.PostfixTemplates.Settings;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.PostfixTemplates
{
  public abstract class PostfixTemplatesManager<TPostfixTemplateContext> : IPostfixTemplatesManager
    where TPostfixTemplateContext : PostfixTemplateContext
  {
    [NotNull] private readonly IList<PostfixTemplateRegistration> myTemplateInfos;

    protected PostfixTemplatesManager([NotNull] IEnumerable<IPostfixTemplate<TPostfixTemplateContext>> templates)
    {
      var infos = new List<PostfixTemplateRegistration>();

      foreach (var provider in templates)
      {
        var providerType = provider.GetType();
        var attributes = (PostfixTemplateAttribute[]) providerType.GetCustomAttributes(typeof(PostfixTemplateAttribute), inherit: false);
        if (attributes.Length == 1)
        {
          var info = new PostfixTemplateRegistration(provider, attributes[0], providerType.FullName);
          infos.Add(info);
        }
      }

      myTemplateInfos = infos.AsReadOnly();
    }

    private sealed class PostfixTemplateRegistration : IPostfixTemplateMetadata
    {
      public PostfixTemplateRegistration(
        [NotNull] IPostfixTemplate<TPostfixTemplateContext> template, [NotNull] PostfixTemplateAttribute metadata, [NotNull] string providerKey)
      {
        Template = template;
        Metadata = metadata;
        SettingsKey = providerKey;
      }

      [NotNull] public IPostfixTemplate<TPostfixTemplateContext> Template { get; private set; }
      public PostfixTemplateAttribute Metadata { get; private set; }
      public string SettingsKey { get; private set; }
    }

    public IEnumerable<IPostfixTemplateMetadata> AvailableTemplates { get { return myTemplateInfos; } }

    [NotNull]
    public IList<ILookupItem> CollectItems([NotNull] TPostfixTemplateContext context, [CanBeNull] string templateName = null)
    {
      var store = context.Reference.GetSettingsStore();
      var settings = store.GetKey<PostfixTemplatesSettings>(SettingsOptimization.OptimizeDefault);
      settings.DisabledProviders.SnapshotAndFreeze();

      var lookupItems = new List<ILookupItem>();
      foreach (var info in myTemplateInfos)
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

        var lookupItem = info.Template.TryCreateInfo(context);
        if (lookupItem != null)
        {
          lookupItems.Add(lookupItem);
        }
      }

      return lookupItems;
    }

    [CanBeNull]
    public PostfixTemplateContext IsAvailable([CanBeNull] ITreeNode position, [NotNull] PostfixTemplateExecutionContext executionContext)
    {
      if (position == null) return null;

      var contextFactory = myLanguageManager.TryGetService<IPostfixTemplateContextFactory>(position.Language);
      if (contextFactory == null) return null;

      return contextFactory.TryCreate(position, executionContext);
    }
  }
}