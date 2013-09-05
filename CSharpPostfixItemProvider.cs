using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.CSharp.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Feature.Services.CodeCompletion;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion
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
      return completionType == CodeCompletionType.AutomaticCompletion
          || completionType == CodeCompletionType.BasicCompletion;
    }

#if RESHARPER8
    public override bool IsAvailableEx(
      CodeCompletionType[] codeCompletionTypes, CSharpCodeCompletionContext specificContext)
    {
      // enable in double basic completion
      return codeCompletionTypes.Length <= 2;
    }
#endif

    protected override bool AddLookupItems(
      CSharpCodeCompletionContext context, GroupedItemsCollector collector)
    {
      var node = context.NodeInFile;
      if (node == null) return false;

      ReparsedCodeCompletionContext reparsedContext = null;

      // foo.{caret} var bar = ...; fuck my life
      var referenceName = node.Parent as IReferenceName;
      if (referenceName != null)
      {
        reparsedContext = context.UnterminatedContext;
        node = reparsedContext.TreeNode;
        if (node == null) return false;
      }

      var completionType = context.BasicContext.CodeCompletionType;
      var forceMode = (completionType == CodeCompletionType.BasicCompletion);
      var lookupItemsOwner = context.BasicContext.LookupItemsOwner;
      var executionContext = new PostfixExecutionContext(
        context.PsiModule, lookupItemsOwner, reparsedContext);

      var items = myTemplatesManager.GetAvailableItems(node, forceMode, executionContext);

      ICollection<string> idsToRemove = EmptyList<string>.InstanceList;

#if RESHARPER8
      var parameters = context.BasicContext.Parameters;
      if (forceMode && parameters.CodeCompletionTypes.Length > 1)
      {
        idsToRemove = new JetHashSet<string>(System.StringComparer.Ordinal);

        var autoItems = myTemplatesManager.GetAvailableItems(node, false, executionContext);

        if (autoItems.Count > 0)
          foreach (var lookupItem in autoItems)
            idsToRemove.Add(lookupItem.Identity);
      }
#endif

      foreach (var lookupItem in items)
        if (!idsToRemove.Contains(lookupItem.Identity))
          collector.AddAtDefaultPlace(lookupItem);

      return (items.Count > 0);
    }

    // todo: transform?
    // todo: can items be hidden by .ForEach and friends?
  }
}