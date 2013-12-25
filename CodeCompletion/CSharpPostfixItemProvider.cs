using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.Settings;
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
    [NotNull] private readonly PostfixTemplatesManager myTemplatesManager;

    public CSharpPostfixItemProvider([NotNull] PostfixTemplatesManager templatesManager)
    {
      myTemplatesManager = templatesManager;
    }

    protected override bool IsAvailable(CSharpCodeCompletionContext context)
    {
      var completionType = context.BasicContext.CodeCompletionType;
      return completionType == CodeCompletionType.AutomaticCompletion ||
             completionType == CodeCompletionType.BasicCompletion;
    }

    public override bool IsAvailableEx(
      CodeCompletionType[] codeCompletionTypes, CSharpCodeCompletionContext specificContext)
    {
      return codeCompletionTypes.Length <= 2;
    }

    protected override bool AddLookupItems(
      CSharpCodeCompletionContext context, GroupedItemsCollector collector)
    {
      var settingsStore = context.BasicContext.File.GetSettingsStore();
      if (!settingsStore.GetValue(PostfixSettingsAccessor.ShowPostfixItems))
        return false;

      // todo: plz prettify me somehow!
      PostfixExecutionContext executionContext;
      PostfixTemplateContext postfixContext;
      ITreeNode position;

      {
        executionContext = ReparsedPostfixExecutionContext.Create(context, context.UnterminatedContext, "__");
        position = context.UnterminatedContext.TreeNode;
        postfixContext = myTemplatesManager.IsAvailable(position, executionContext);

        if (postfixContext == null) // try unterminated context if terminated sucks
        {
          executionContext = ReparsedPostfixExecutionContext.Create(context, context.TerminatedContext, "__;");
          position = context.TerminatedContext.TreeNode;
          postfixContext = myTemplatesManager.IsAvailable(position, executionContext);
        }
      }

      if (postfixContext == null || postfixContext.Expressions.Count == 0) return false;

      var items = myTemplatesManager.GetAvailableItems(postfixContext);
      if (items.Count == 0) return false;

      ICollection<string> toRemove = EmptyList<string>.InstanceList;

      // double completion support
      var parameters = context.BasicContext.Parameters;
      if (executionContext.IsForceMode && parameters.CodeCompletionTypes.Length > 1)
      {
        var firstCompletion = parameters.CodeCompletionTypes[0];
        if (firstCompletion != CodeCompletionType.AutomaticCompletion) return false;

        // run postfix templates like non-force mode enabled
        var nonForceExecutionContext = executionContext.WithForceMode(false);

        var forcedContext = myTemplatesManager.IsAvailable(position, nonForceExecutionContext);
        if (forcedContext != null)
        {
          var automaticPostfixItems = myTemplatesManager.GetAvailableItems(forcedContext);
          if (automaticPostfixItems.Count > 0)
          {
            toRemove = new JetHashSet<string>(StringComparer.Ordinal);
            foreach (var lookupItem in automaticPostfixItems)
            {
              toRemove.Add(lookupItem.Identity);
            }
          }
        }
      }

      foreach (var lookupItem in items)
      {
        if (toRemove.Contains(lookupItem.Identity)) continue;

        collector.AddAtDefaultPlace(lookupItem);
      }

      return (items.Count > 0);
    }
  }
}