using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.CodeCompletion;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Feature.Services.CSharp.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Tree;
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

    // move out?
    private sealed class ReparsedPostfixExecutionContext : PostfixExecutionContext
    {
      [NotNull] private readonly ReparsedCodeCompletionContext myReparsedContext;

      private ReparsedPostfixExecutionContext(
        bool isForceMode, [NotNull] IPsiModule psiModule,
        [NotNull] ILookupItemsOwner lookupItemsOwner,
        [NotNull] ReparsedCodeCompletionContext reparsedContext,
        [NotNull] string reparseString)
        : base(isForceMode, psiModule, lookupItemsOwner, reparseString)
      {
        myReparsedContext = reparsedContext;
      }

      public override DocumentRange GetDocumentRange(ITreeNode treeNode)
      {
        return myReparsedContext.ToDocumentRange(treeNode);
      }

      [NotNull]
      public static PostfixExecutionContext Create(
        [NotNull] CSharpCodeCompletionContext completionContext,
        [NotNull] ReparsedCodeCompletionContext reparsedContext,
        [NotNull] string reparseString)
      {
        var completionType = completionContext.BasicContext.CodeCompletionType;
        var isForceMode = (completionType == CodeCompletionType.BasicCompletion);
        var lookupItemsOwner = completionContext.BasicContext.LookupItemsOwner;

        return new ReparsedPostfixExecutionContext(
          isForceMode, completionContext.PsiModule,
          lookupItemsOwner, reparsedContext, reparseString);
      }
    }

    protected override bool AddLookupItems(
      CSharpCodeCompletionContext context, GroupedItemsCollector collector)
    {
      var executionContext = ReparsedPostfixExecutionContext.Create(context, context.UnterminatedContext, "__");

      var treeNode = context.UnterminatedContext.TreeNode;
      if (treeNode == null) return false;

      // todo: sometimes prefer unterminated context!

      var items = myTemplatesManager.GetAvailableItems(treeNode, executionContext);

      ICollection<string> idsToRemove = EmptyList<string>.InstanceList;

      var parameters = context.BasicContext.Parameters;
      if (executionContext.IsForceMode && parameters.CodeCompletionTypes.Length > 1)
      {
        idsToRemove = new JetHashSet<string>(StringComparer.Ordinal);

        var firstCompletion = parameters.CodeCompletionTypes[0];
        if (firstCompletion != CodeCompletionType.AutomaticCompletion)
          return false;

        var autoItems = myTemplatesManager.GetAvailableItems(treeNode, executionContext);
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