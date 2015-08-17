using JetBrains.Annotations;
using JetBrains.DataFlow;
using JetBrains.ReSharper.Feature.Services.CSharp.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.PostfixTemplates.Contexts;
using JetBrains.ReSharper.PostfixTemplates.Contexts.CSharp;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;

namespace JetBrains.ReSharper.PostfixTemplates.CodeCompletion
{
  [Language(typeof(CSharpLanguage))]
  public class CSharpPostfixTemplatesItemProvider : PostfixTemplatesItemProviderBase<CSharpCodeCompletionContext>
  {
    [NotNull] private readonly CSharpPostfixTemplateContextFactory myPostfixContextFactory;

    public CSharpPostfixTemplatesItemProvider(
      [NotNull] PostfixTemplatesManager templatesManager, [NotNull] CSharpPostfixTemplateContextFactory postfixContextFactory)
      : base(templatesManager)
    {
      myPostfixContextFactory = postfixContextFactory;
    }

    protected override PostfixTemplateContext TryCreate(CSharpCodeCompletionContext codeCompletionContext)
    {
      var unterminatedContext = codeCompletionContext.UnterminatedContext;
      var executionContext = new CodeCompletionPostfixExecutionContext(codeCompletionContext.BasicContext, unterminatedContext, "__");
      var postfixContext = myPostfixContextFactory.TryCreate(unterminatedContext.TreeNode, executionContext);

      if (postfixContext == null) // try unterminated context if terminated sucks
      {
        var terminatedContext = codeCompletionContext.TerminatedContext;
        executionContext = new CodeCompletionPostfixExecutionContext(codeCompletionContext.BasicContext, terminatedContext, "__;");
        return myPostfixContextFactory.TryCreate(terminatedContext.TreeNode, executionContext);
      }

      return null;
    }
  }
}