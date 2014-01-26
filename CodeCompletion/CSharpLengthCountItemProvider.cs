using System;
using JetBrains.ReSharper.Feature.Services.CodeCompletion;
using JetBrains.ReSharper.Feature.Services.CSharp.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;

namespace JetBrains.ReSharper.PostfixTemplates.CodeCompletion
{
  [Language(typeof(CSharpLanguage))]
  public class CSharpLengthCountItemProvider : CSharpItemsProviderBase<CSharpCodeCompletionContext>
  {
    protected override bool IsAvailable(CSharpCodeCompletionContext context)
    {
      var completionType = context.BasicContext.CodeCompletionType;
      return completionType == CodeCompletionType.AutomaticCompletion
          || completionType == CodeCompletionType.BasicCompletion;
    }

    protected override bool AddLookupItems(CSharpCodeCompletionContext context, GroupedItemsCollector collector)
    {
      var referenceExpression = CommonUtils.FindReferenceExpression(context.UnterminatedContext) ??
                                CommonUtils.FindReferenceExpression(context.TerminatedContext);

      return false;
    }

    protected override void TransformItems(CSharpCodeCompletionContext context, GroupedItemsCollector collector)
    {
      var referenceExpression = CommonUtils.FindReferenceExpression(context.UnterminatedContext) ??
                                CommonUtils.FindReferenceExpression(context.TerminatedContext);
      if (referenceExpression == null) return;

      foreach (var lookupItem in collector.Items)
      {
        if (lookupItem.Identity == "Length")
        {
          GC.KeepAlive(this);
        }
      }
    }
  }
}