﻿using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.DataFlow;
using JetBrains.ReSharper.Feature.Services.CodeCompletion;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Feature.Services.CSharp.CodeCompletion;
using JetBrains.ReSharper.Feature.Services.CSharp.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.PostfixTemplates.Settings;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
#if RESHARPER8
using JetBrains.ReSharper.Feature.Services.Lookup;
#elif RESHARPER9
using JetBrains.ReSharper.Features.Intellisense.CodeCompletion.CSharp.Rules;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
#endif

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
      return context.BasicContext.IsAutoOrBasicCompletionType();
    }

#if !RESHARPER91

    public override bool IsAvailableEx(
      [NotNull] CodeCompletionType[] codeCompletionTypes, [NotNull] CSharpCodeCompletionContext specificContext)
    {
      return codeCompletionTypes.Length <= 2;
    }

#endif

    protected override bool AddLookupItems(CSharpCodeCompletionContext context, GroupedItemsCollector collector)
    {
      var completionContext = context.BasicContext;

      var settingsStore = completionContext.File.GetSettingsStore();
      if (!settingsStore.GetValue(PostfixSettingsAccessor.ShowPostfixItems)) return false;

      var unterminatedContext = context.UnterminatedContext;
      var executionContext = new ReparsedPostfixExecutionContext(myLifetime, completionContext, unterminatedContext, "__");
      var postfixContext = myTemplatesManager.IsAvailable(unterminatedContext.TreeNode, executionContext);

      if (postfixContext == null) // try unterminated context if terminated sucks
      {
        var terminatedContext = context.TerminatedContext;
        executionContext = new ReparsedPostfixExecutionContext(myLifetime, completionContext, terminatedContext, "__;");
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
#if RESHARPER91
        if (parameters.IsAutomaticCompletion) return false;
#else
        var firstCompletion = parameters.CodeCompletionTypes[0];
        if (firstCompletion != CodeCompletionType.AutomaticCompletion) return false;
#endif

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
#if RESHARPER91
          collector.Add(lookupItem);
#else
          collector.AddToTop(lookupItem);
#endif
        }
        else
        {
          collector.AddSomewhere(lookupItem);
        }
      }

      return (lookupItems.Count > 0);
    }
  }
}