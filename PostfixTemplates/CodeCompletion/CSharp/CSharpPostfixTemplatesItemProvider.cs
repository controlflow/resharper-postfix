using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.CSharp.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.PostfixTemplates.Contexts;
using JetBrains.ReSharper.PostfixTemplates.Contexts.CSharp;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;

// todo: additional namespace filter

namespace JetBrains.ReSharper.PostfixTemplates.CodeCompletion.CSharp
{
  [Language(typeof(CSharpLanguage))]
  public class CSharpPostfixTemplatesItemProvider : PostfixTemplatesItemProviderBase<CSharpCodeCompletionContext, CSharpPostfixTemplateContext>
  {
    [NotNull] private readonly CSharpPostfixTemplateContextFactory myPostfixContextFactory;

    public CSharpPostfixTemplatesItemProvider(
      [NotNull] CSharpPostfixTemplatesManager templatesManager, [NotNull] CSharpPostfixTemplateContextFactory postfixContextFactory) : base(templatesManager)
    {
      myPostfixContextFactory = postfixContextFactory;
    }

    protected override PostfixTemplateContext TryCreatePostfixContext(CSharpCodeCompletionContext codeCompletionContext)
    {
      var completionContext = codeCompletionContext.BasicContext;

      var unterminatedContext = codeCompletionContext.UnterminatedContext;
      if (unterminatedContext.TreeNode != null)
      {
        var executionContext = new CodeCompletionPostfixExecutionContext(completionContext, unterminatedContext, "__");
        var postfixContext = myPostfixContextFactory.TryCreate(unterminatedContext.TreeNode, executionContext);
        if (postfixContext != null) return postfixContext;
      }

      // try unterminated context if terminated sucks
      var terminatedContext = codeCompletionContext.TerminatedContext;
      if (terminatedContext.TreeNode != null)
      {
        var executionContext = new CodeCompletionPostfixExecutionContext(completionContext, terminatedContext, "__;");
        var postfixContext = myPostfixContextFactory.TryCreate(terminatedContext.TreeNode, executionContext);
        if (postfixContext != null) return postfixContext;
      }

      return null;
    }
  }
}