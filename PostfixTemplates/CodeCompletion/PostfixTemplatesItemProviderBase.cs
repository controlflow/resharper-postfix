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
      var completionTypes = context.BasicContext.Parameters.CodeCompletionTypes;

      switch (completionTypes.Length)
      {
        case 1:
          return completionTypes[0] == CodeCompletionType.BasicCompletion;

        case 2:
          return completionTypes[0] == CodeCompletionType.BasicCompletion &&
                 completionTypes[1] == CodeCompletionType.BasicCompletion;

        default:
          return false;
      }
    }

    public override CompletionMode SupportedCompletionMode { get { return CompletionMode.All; } }

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

      // additional semantic checks
      if (!postfixContext.IsSemanticallyMakeSence()) return false;

      var lookupItems = BuildLookupItems(postfixContext, completionContext);
      if (lookupItems.Count == 0) return false;

      ICollection<string> toRemove = EmptyList<string>.InstanceList;

      // double completion support
      if (completionContext.Parameters.CodeCompletionTypes.Length > 1)
      {
        // run postfix templates like we are in auto completion
        postfixContext.ExecutionContext.IsPreciseMode = true; // ewww mutability

        var automaticPostfixItems = BuildLookupItems(postfixContext, completionContext);
        if (automaticPostfixItems.Count > 0)
        {
          toRemove = new JetHashSet<string>(StringComparer.Ordinal);

          foreach (var lookupItem in automaticPostfixItems)
            toRemove.Add(lookupItem.Info.Text);
        }
      }

      foreach (var lookupItem in lookupItems)
      {
        if (!toRemove.Contains(lookupItem.Info.Text))
          collector.Add(lookupItem);
      }

      return (lookupItems.Count > 0);
    }

    [NotNull]
    private IList<LookupItem<PostfixTemplateInfo>> BuildLookupItems([NotNull] TPostfixTemplateContext context, [NotNull] CodeCompletionContext completionContext)
    {
      var items = new LocalList<LookupItem<PostfixTemplateInfo>>();
      var multiplier = completionContext.Parameters.CodeCompletionTypes.Length;

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

        postfixTemplateInfo.Multiplier = multiplier;

        items.Add(LookupItemFactory.CreateLookupItem(postfixTemplateInfo)
          .WithMatcher(item => new PostfixTemplateMatcher(item.Info))
          .WithBehavior(item => templateProvider.CreateBehavior(item.Info))
          .WithPresentation(item => new PostfixTemplatePresentation(item.Info.Text)));
      }

      return items.ResultingList();
    }
  }
}