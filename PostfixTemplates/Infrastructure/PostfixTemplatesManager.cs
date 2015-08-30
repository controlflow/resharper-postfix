using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.PostfixTemplates.Contexts;
using JetBrains.ReSharper.PostfixTemplates.Settings;
using JetBrains.ReSharper.PostfixTemplates.Templates;
using JetBrains.TextControl;
using JetBrains.Util;

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

    public sealed class PostfixTemplateRegistration : IPostfixTemplateMetadata
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

    [NotNull, Pure]
    public IEnumerable<PostfixTemplateRegistration> GetEnabledTemplates([NotNull] TPostfixTemplateContext context)
    {
      // snapshot settings of disabled providers
      var settingsStore = context.ExecutionContext.SettingsStore;
      var settingsKey = settingsStore.GetKey<PostfixTemplatesSettings>(SettingsOptimization.OptimizeDefault);
      settingsKey.DisabledProviders.SnapshotAndFreeze();

      foreach (var templateRegistration in myTemplateInfos)
      {
        var isEnabled = settingsKey.DisabledProviders.GetIndexedValue(
          templateRegistration.SettingsKey, !templateRegistration.Metadata.DisabledByDefault);

        if (!isEnabled) continue; // filter out disabled providers

        yield return templateRegistration;
      }
    }

    public bool IsTemplateAvailableByName(PostfixTemplateContext context, string templateName)
    {
      var specificContext = (TPostfixTemplateContext) context;

      foreach (var templateRegistration in GetEnabledTemplates(specificContext))
      {
        if (string.Equals(templateRegistration.Metadata.TemplateName, templateName, StringComparison.Ordinal))
        {
          var templateInfo = templateRegistration.Template.TryCreateInfo(specificContext);
          if (templateInfo != null)
          {
            return true;
          }
        }
      }

      return false;
    }

    public void ExecuteTemplateByName(PostfixTemplateContext context, string templateName, ITextControl textControl, TextRange nameRange)
    {
      var specificContext = (TPostfixTemplateContext) context;

      foreach (var templateRegistration in GetEnabledTemplates(specificContext))
      {
        if (string.Equals(templateRegistration.Metadata.TemplateName, templateName, StringComparison.Ordinal))
        {
          var postfixTemplateInfo = templateRegistration.Template.TryCreateInfo(specificContext);
          if (postfixTemplateInfo != null)
          {
            var behavior = templateRegistration.Template.CreateBehavior(postfixTemplateInfo);
            behavior.Accept(
              textControl, nameRange, LookupItemInsertType.Insert, Suffix.Empty, context.ExecutionContext.Solution, false);
          }
        }
      }
    }
  }
}