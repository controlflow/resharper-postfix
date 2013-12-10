using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.CodeCompletion;
using JetBrains.ReSharper.Feature.Services.CSharp.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.Util;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.CodeCompletion
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
      // enable in double basic completion
      return codeCompletionTypes.Length <= 2;
    }

    protected override bool AddLookupItems(
      CSharpCodeCompletionContext context, GroupedItemsCollector collector)
    {
      var node = context.NodeInFile;
      if (node == null) return false;

      var completionType = context.BasicContext.CodeCompletionType;
      var forceMode = (completionType == CodeCompletionType.BasicCompletion);
      var lookupItemsOwner = context.BasicContext.LookupItemsOwner;
      var executionContext = new PostfixExecutionContext(
        context.PsiModule, lookupItemsOwner, context.UnterminatedContext);

      var treeNode = context.UnterminatedContext.TreeNode;
      if (treeNode == null) return false;

      var items = myTemplatesManager.GetAvailableItems(treeNode, forceMode, executionContext);

      ICollection<string> idsToRemove = EmptyList<string>.InstanceList;

      var parameters = context.BasicContext.Parameters;
      if (forceMode && parameters.CodeCompletionTypes.Length > 1)
      {
        idsToRemove = new JetHashSet<string>(System.StringComparer.Ordinal);

        var firstCompletion = parameters.CodeCompletionTypes[0];
        if (firstCompletion != CodeCompletionType.AutomaticCompletion)
          return false;

        var autoItems = myTemplatesManager.GetAvailableItems(node, false, executionContext);
        if (autoItems.Count > 0)
        {
          foreach (var lookupItem in autoItems)
          {
            idsToRemove.Add(lookupItem.Identity);
          }
        }
      }

      foreach (var lookupItem in items)
      {
        if (!idsToRemove.Contains(lookupItem.Identity))
        {
          collector.AddAtDefaultPlace(lookupItem);
        }
      }

      return (items.Count > 0);
    }
  }
}