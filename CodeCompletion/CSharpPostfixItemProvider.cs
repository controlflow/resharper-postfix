using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.DataFlow;
using JetBrains.ReSharper.Feature.Services.CodeCompletion;
using JetBrains.ReSharper.Feature.Services.CSharp.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Feature.Services.Lookup;
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

    public CSharpPostfixItemProvider([NotNull] Lifetime lifetime,
                                     [NotNull] PostfixTemplatesManager templatesManager)
    {
      myLifetime = lifetime;
      myTemplatesManager = templatesManager;
    }

    protected override bool IsAvailable(CSharpCodeCompletionContext context)
    {
      var completionType = context.BasicContext.CodeCompletionType;
      return completionType == CodeCompletionType.AutomaticCompletion ||
             completionType == CodeCompletionType.BasicCompletion;
    }

    public override bool IsAvailableEx([NotNull] CodeCompletionType[] codeCompletionTypes,
                                       [NotNull] CSharpCodeCompletionContext specificContext)
    {
      return codeCompletionTypes.Length <= 2;
    }

    protected override bool AddLookupItems(CSharpCodeCompletionContext context,
                                           GroupedItemsCollector collector)
    {
      var settingsStore = context.BasicContext.File.GetSettingsStore();
      if (!settingsStore.GetValue(PostfixSettingsAccessor.ShowPostfixItems))
        return false;

      var executionContext = new ReparsedPostfixExecutionContext(
        myLifetime, context.BasicContext, context.UnterminatedContext, "__");
      var postfixContext = myTemplatesManager.IsAvailable(
        context.UnterminatedContext.TreeNode, executionContext);

      if (postfixContext == null) // try unterminated context if terminated sucks
      {
        executionContext = new ReparsedPostfixExecutionContext(
          myLifetime, context.BasicContext, context.TerminatedContext, "__;");
        postfixContext = myTemplatesManager.IsAvailable(
          context.TerminatedContext.TreeNode, executionContext);
      }

      if (postfixContext == null || postfixContext.Expressions.Count == 0) return false;

      var lookupItems = myTemplatesManager.CollectItems(postfixContext);
      if (lookupItems.Count == 0) return false;

      ICollection<string> toRemove = EmptyList<string>.InstanceList;

      // double completion support
      var parameters = context.BasicContext.Parameters;
      if (executionContext.IsForceMode && parameters.CodeCompletionTypes.Length > 1)
      {
        var firstCompletion = parameters.CodeCompletionTypes[0];
        if (firstCompletion != CodeCompletionType.AutomaticCompletion) return false;

        // run postfix templates like we are in auto completion
        executionContext.IsAutoCompletion = true;

        var automaticPostfixItems = myTemplatesManager.CollectItems(postfixContext);
        if (automaticPostfixItems.Count > 0)
        {
          toRemove = new JetHashSet<string>(StringComparer.Ordinal);
          foreach (var lookupItem in automaticPostfixItems)
          {
            toRemove.Add(lookupItem.Identity);
          }
        }
      }

      foreach (var lookupItem in lookupItems)
      {
        if (toRemove.Contains(lookupItem.Identity)) continue;

        collector.AddAtDefaultPlace(lookupItem);
      }

      return (lookupItems.Count > 0);
    }
  }
}