using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.ReSharper.Feature.Services.CodeCompletion;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.BaseInfrastructure;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.PostfixTemplates.Contexts;
using JetBrains.ReSharper.PostfixTemplates.LookupItems;
using JetBrains.ReSharper.PostfixTemplates.Settings;
using JetBrains.Util;

namespace JetBrains.ReSharper.PostfixTemplates.CodeCompletion
{
  // todo: add check for 'if (myHotspotSessionExecutor.CurrentSession != null) return false;'?

  public abstract class PostfixTemplatesItemProviderBase<TCodeCompletionContext, TPostfixTemplateContext> : ItemsProviderOfSpecificContext<TCodeCompletionContext>
    where TCodeCompletionContext : class, ISpecificCodeCompletionContext
    where TPostfixTemplateContext : PostfixTemplateContext
  {
    [NotNull] private readonly PostfixTemplatesManager<TPostfixTemplateContext> myTemplatesManager;

    protected PostfixTemplatesItemProviderBase([NotNull] PostfixTemplatesManager<TPostfixTemplateContext> templatesManager)
    {
      myTemplatesManager = templatesManager;
    }

    [CanBeNull] protected abstract PostfixTemplateContext TryCreatePostfixContext([NotNull] TCodeCompletionContext codeCompletionContext);

    protected sealed override bool IsAvailable(TCodeCompletionContext context)
    {
      return context.BasicContext.CodeCompletionType == CodeCompletionType.BasicCompletion;
    }

    protected sealed override bool AddLookupItems(TCodeCompletionContext context, GroupedItemsCollector collector)
    {
      var completionContext = context.BasicContext;

      // check postfix is disabled for code completion
      var settingsStore = completionContext.ContextBoundSettingsStore;
      if (!settingsStore.GetValue(PostfixTemplatesSettingsAccessor.ShowPostfixItems)) return false;

      var postfixContext = TryCreatePostfixContext(context) as TPostfixTemplateContext;
      if (postfixContext == null) return false;

      // check if there is no expression detected and do nothing if so
      if (postfixContext.AllExpressions.Count == 0) return false;

      var lookupItems = BuildLookupItems(postfixContext).ToList();
      if (lookupItems.Count == 0) return false;

      ICollection<string> toRemove = EmptyList<string>.InstanceList;

      // double completion support
      var completionParameters = completionContext.Parameters;
      var isDoubleCompletion = (completionParameters.CodeCompletionTypes.Length > 1);

      if (!completionParameters.IsAutomaticCompletion && isDoubleCompletion)
      {
        if (completionParameters.IsAutomaticCompletion) return false;

        // run postfix templates like we are in auto completion
        

        // todo: re-create execution context with IsAuto disabled?

        var automaticPostfixItems = BuildLookupItems(postfixContext).ToList();
        if (automaticPostfixItems.Count > 0)
        {
          toRemove = new JetHashSet<string>(StringComparer.Ordinal);

          foreach (var lookupItem in automaticPostfixItems)
          {
            toRemove.Add(lookupItem.Info.Shortcut);
          }
        }
      }

      foreach (var lookupItem in lookupItems)
      {
        if (toRemove.Contains(lookupItem.Info.Shortcut)) continue;

        // todo: add to bottom in double completion
        collector.Add(lookupItem);
      }

      return (lookupItems.Count > 0);
    }

    [NotNull]
    private IEnumerable<LookupItem<PostfixTemplateInfo>> BuildLookupItems([NotNull] TPostfixTemplateContext context)
    {
      foreach (var templateRegistration in myTemplatesManager.GetEnabledTemplates(context))
      {
        var templateProvider = templateRegistration.Template;

        var postfixTemplateInfo = templateProvider.TryCreateInfo(context);
        if (postfixTemplateInfo == null) continue;

        var templateName = templateRegistration.Metadata.TemplateName;

        Assertion.Assert(
          string.Equals(templateName, postfixTemplateInfo.Text, StringComparison.Ordinal),
          "Template text '{0}' should match declared template name '{1}'",
          postfixTemplateInfo.Text, templateName);

        yield return LookupItemFactory
          .CreateLookupItem(postfixTemplateInfo)
          .WithMatcher(item => new PostfixTemplateMatcher(item.Info))
          .WithBehavior(item => templateProvider.CreateBehavior(item.Info))
          .WithPresentation(item => new PostfixTemplatePresentation(item.Info.Text));
      }
    }
  }
}