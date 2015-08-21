using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.ReSharper.Feature.Services.CodeCompletion;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.PostfixTemplates.Contexts;
using JetBrains.ReSharper.PostfixTemplates.Settings;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.PostfixTemplates.CodeCompletion
{
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
      if (!settingsStore.GetValue(PostfixSettingsAccessor.ShowPostfixItems)) return false;

      var postfixContext = TryCreatePostfixContext(context);
      if (postfixContext == null) return false;

      // nothing to check :(
      if (postfixContext.AllExpressions.Count > 0) return false;

      var lookupItems = myTemplatesManager.CollectItems(postfixContext);
      if (lookupItems.Count == 0) return false;




      ICollection<string> toRemove = EmptyList<string>.InstanceList;

      // double completion support
      var parameters = completionContext.Parameters;
      var isDoubleCompletion = (parameters.CodeCompletionTypes.Length > 1);

      if (!parameters.IsAutomaticCompletion && isDoubleCompletion)
      {
        if (parameters.IsAutomaticCompletion) return false;

        // run postfix templates like we are in auto completion


        var automaticPostfixItems = myTemplatesManager.CollectItems(postfixContext);
        if (automaticPostfixItems.Count > 0)
        {
          toRemove = new JetHashSet<string>(StringComparer.Ordinal);

          foreach (var lookupItem in automaticPostfixItems)
            toRemove.Add(lookupItem.Placement.OrderString);
        }
      }

      foreach (var lookupItem in lookupItems)
      {
        if (toRemove.Contains(lookupItem.Placement.OrderString)) continue;

        if (isDoubleCompletion)
        {
          collector.Add(lookupItem);
        }
        else
        {
          collector.Add(lookupItem);
        }
      }

      return (lookupItems.Count > 0);
    }
  }
}