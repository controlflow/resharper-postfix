using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.PostfixTemplates.Contexts;
using JetBrains.ReSharper.PostfixTemplates.Settings;
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
        // filter out disabled providers
        bool isEnabled;
        if (!settingsKey.DisabledProviders.TryGet(templateRegistration.SettingsKey, out isEnabled))
        {
          isEnabled = !templateRegistration.Metadata.DisabledByDefault;
        }

        if (!isEnabled) continue;

        yield return templateRegistration;
      }
    }

    public void GetTemplateByName([NotNull] TPostfixTemplateContext context, [NotNull] string templateName)
    {
      foreach (var templateRegistration in GetEnabledTemplates(context))
      {
        if (string.Equals(templateRegistration.Metadata.TemplateName, templateName, StringComparison.Ordinal))
        {
          var postfixTemplateInfo = templateRegistration.Template.TryCreateInfo(context);
          if (postfixTemplateInfo != null)
          {
            var behavior = templateRegistration.Template.CreateBehavior(postfixTemplateInfo);

            // todo: return or execute smth like :\
            Action<ITextControl, TextRange, ISolution> a = (tc, r, solution) =>
              behavior.Accept(tc, r, LookupItemInsertType.Replace, Suffix.Empty, solution, false);
          }
        }
      }
    }

    //[NotNull]
    //public IList<ILookupItem> CollectItems([NotNull] TPostfixTemplateContext context, [CanBeNull] string templateName = null)
    //{
    //  var lookupItems = new List<ILookupItem>();
    //  foreach (var templateRegistration in GetEnabledTemplates(context))
    //  {
    //    if (templateName != null)
    //    {
    //      var name = templateRegistration.Metadata.TemplateName;
    //      if (!string.Equals(templateName, name, StringComparison.Ordinal)) continue;
    //    }
    //
    //    var templateProvider = templateRegistration.Template;
    //
    //    var postfixTemplateInfo = templateProvider.TryCreateInfo(context);
    //    if (postfixTemplateInfo == null) continue;
    //
    //    Assertion.Assert(templateRegistration.Metadata.TemplateName == postfixTemplateInfo.Text, "TODO: AAAA");
    //
    //    var lookupItem = LookupItemFactory
    //      .CreateLookupItem(postfixTemplateInfo)
    //      .WithMatcher(x => new PostfixTemplateMatcher(x.Info))
    //      .WithBehavior(x => templateProvider.CreateBehavior(x.Info))
    //      .WithPresentation(x => new PostfixTemplatePresentation(x.Info.Text));
    //
    //    lookupItems.Add(lookupItem);
    //  }
    //
    //  return lookupItems;
    //}
  }
}