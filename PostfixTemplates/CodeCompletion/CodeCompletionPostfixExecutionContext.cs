using JetBrains.Annotations;
using JetBrains.DataFlow;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.PostfixTemplates.Contexts
{
  internal sealed class CodeCompletionPostfixExecutionContext : PostfixExecutionContext
  {
    [NotNull] private readonly ReparsedCodeCompletionContext myReparsedContext;

    public CodeCompletionPostfixExecutionContext(
      [NotNull] Lifetime lifetime, [NotNull] CodeCompletionContext context,
      [NotNull] ReparsedCodeCompletionContext reparsedContext, [NotNull] string reparseString)
      : base(lifetime, context.Solution, context.TextControl, context.LookupItemsOwner,
             reparseString, isAutoCompletion: context.Parameters.IsAutomaticCompletion)
    {
      myReparsedContext = reparsedContext;
    }

    public override DocumentRange GetDocumentRange(ITreeNode treeNode)
    {
      return myReparsedContext.ToDocumentRange(treeNode);
    }
  }
}