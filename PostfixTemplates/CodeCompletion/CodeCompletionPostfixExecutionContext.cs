using JetBrains.Annotations;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.PostfixTemplates.Contexts;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.PostfixTemplates.CodeCompletion
{
  internal sealed class CodeCompletionPostfixExecutionContext : PostfixTemplateExecutionContext
  {
    [NotNull] private readonly ReparsedCodeCompletionContext myReparsedContext;

    public CodeCompletionPostfixExecutionContext(
      [NotNull] CodeCompletionContext context, [NotNull] ReparsedCodeCompletionContext reparsedContext, [NotNull] string reparseString)
      : base(context.Solution, context.TextControl, reparseString, isPreciseMode: context.Parameters.IsAutomaticCompletion)
    {
      myReparsedContext = reparsedContext;
    }

    public override DocumentRange GetDocumentRange(ITreeNode treeNode)
    {
      return myReparsedContext.ToDocumentRange(treeNode);
    }
  }
}