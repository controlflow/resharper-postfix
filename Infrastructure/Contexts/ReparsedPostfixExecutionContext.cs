using JetBrains.Annotations;
using JetBrains.DataFlow;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.CodeCompletion;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.PostfixTemplates
{
  internal sealed class ReparsedPostfixExecutionContext : PostfixExecutionContext
  {
    [NotNull] private readonly ReparsedCodeCompletionContext myReparsedContext;

    public ReparsedPostfixExecutionContext([NotNull] Lifetime lifetime,
                                           [NotNull] CodeCompletionContext context,
                                           [NotNull] ReparsedCodeCompletionContext reparsedContext,
                                           [NotNull] string reparseString)
      : base(lifetime, context.Solution, context.TextControl, context.LookupItemsOwner,
             reparseString, context.CodeCompletionType == CodeCompletionType.AutomaticCompletion)
    {
      myReparsedContext = reparsedContext;
    }

    public override DocumentRange GetDocumentRange(ITreeNode treeNode)
    {
      return myReparsedContext.ToDocumentRange(treeNode);
    }
  }
}