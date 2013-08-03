using JetBrains.Annotations;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion
{
  public static class CommonUtils
  {
    public static DocumentRange ToDocumentRange(
      [CanBeNull] this ReparsedCodeCompletionContext context, [NotNull] ITreeNode treeNode)
    {
      var documentRange = treeNode.GetDocumentRange();
      if (context == null) return documentRange;

      var originalRange = context.ToDocumentRange(treeNode.GetTreeTextRange());
      return new DocumentRange(documentRange.Document, originalRange);
    }
  }
}