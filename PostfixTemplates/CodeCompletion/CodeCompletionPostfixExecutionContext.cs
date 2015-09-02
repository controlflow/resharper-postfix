using JetBrains.Annotations;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.PostfixTemplates.Contexts;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.PostfixTemplates.CodeCompletion
{
  public class CodeCompletionPostfixExecutionContext : PostfixTemplateExecutionContext
  {
    [NotNull] private readonly ReparsedCodeCompletionContext myReparsedContext;

    public CodeCompletionPostfixExecutionContext(
      [NotNull] CodeCompletionContext context, [NotNull] ReparsedCodeCompletionContext reparsedContext, [NotNull] string reparseString)
      : base(solution: context.Solution,
             textControl: context.TextControl,
             settingsStore: context.ContextBoundSettingsStore,
             reparseString: reparseString,
           //isPreciseMode: context.Parameters.CodeCompletionTypes.Length == 1
             isPreciseMode: context.Parameters.IsAutomaticCompletion
            )
    {
      myReparsedContext = reparsedContext;
    }

    public override DocumentRange GetDocumentRange(ITreeNode treeNode)
    {
      return myReparsedContext.ToDocumentRange(treeNode);
    }
  }
}