using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.DataFlow;
using JetBrains.ReSharper.Feature.Services.CodeCompletion;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.Feature.Services.CSharp.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Features.Intellisense.CodeCompletion.CSharp.Rules;
using JetBrains.ReSharper.PostfixTemplates.Contexts;
using JetBrains.ReSharper.PostfixTemplates.Settings;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.PostfixTemplates.CodeCompletion
{
  [Language(typeof(CSharpLanguage))]
  public class CSharpPostfixItemProvider : CSharpItemsProviderBase<CSharpCodeCompletionContext>
  {
    [NotNull] private readonly Lifetime myLifetime;
    [NotNull] private readonly PostfixTemplatesManager myTemplatesManager;

    public CSharpPostfixItemProvider([NotNull] Lifetime lifetime, [NotNull] PostfixTemplatesManager templatesManager)
    {
      myLifetime = lifetime;
      myTemplatesManager = templatesManager;
    }

    protected override bool IsAvailable(CSharpCodeCompletionContext context)
    {
      return context.BasicContext.CodeCompletionType == CodeCompletionType.BasicCompletion;
    }

    protected override bool AddLookupItems(CSharpCodeCompletionContext context, GroupedItemsCollector collector)
    {
      var completionContext = context.BasicContext;

      var settingsStore = completionContext.File.GetSettingsStore();
      if (!settingsStore.GetValue(PostfixSettingsAccessor.ShowPostfixItems)) return false;

      var unterminatedContext = context.UnterminatedContext;
      var executionContext = new CodeCompletionPostfixExecutionContext(myLifetime, completionContext, unterminatedContext, "__");
      var postfixContext = myTemplatesManager.IsAvailable(unterminatedContext.TreeNode, executionContext);

      if (postfixContext == null) // try unterminated context if terminated sucks
      {
        var terminatedContext = context.TerminatedContext;
        executionContext = new CodeCompletionPostfixExecutionContext(myLifetime, completionContext, terminatedContext, "__;");
        postfixContext = myTemplatesManager.IsAvailable(terminatedContext.TreeNode, executionContext);
      }

      if (postfixContext == null) return false;

      // nothing to check :(
      if (postfixContext.Expressions.Count == 0 && postfixContext.TypeExpression == null) return false;

      var lookupItems = myTemplatesManager.CollectItems(postfixContext);
      if (lookupItems.Count == 0) return false;

      ICollection<string> toRemove = EmptyList<string>.InstanceList;

      // double completion support
      var parameters = completionContext.Parameters;
      var isDoubleCompletion = (parameters.CodeCompletionTypes.Length > 1);

      if (!executionContext.IsAutoCompletion && isDoubleCompletion)
      {
        if (parameters.IsAutomaticCompletion) return false;

        // run postfix templates like we are in auto completion
        executionContext.IsAutoCompletion = true;

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