using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.PostfixTemplates.Contexts
{
  public abstract class PostfixTemplateContext
  {
    [CanBeNull] private IList<PostfixExpressionContext> myAllExpressions;

    protected PostfixTemplateContext([NotNull] ITreeNode reference, [NotNull] PostfixTemplateExecutionContext executionContext)
    {
      Reference = reference;
      PsiModule = reference.GetPsiModule();
      ExecutionContext = executionContext;
    }

    [NotNull] public ITreeNode Reference { get; private set; }
    [NotNull] public IPsiModule PsiModule { get; private set; }

    [NotNull] public PostfixTemplateExecutionContext ExecutionContext { get; private set; }

    // Expressions: 'a', 'a + b.Length', '(a + b.Length)', '(a + b.Length) > 0.var'
    [NotNull, ItemNotNull]
    public IList<PostfixExpressionContext> AllExpressions
    {
      get { return myAllExpressions ?? (myAllExpressions = BuildAllExpressions()); }
    }

    [NotNull, ItemNotNull]
    protected abstract IList<PostfixExpressionContext> BuildAllExpressions();

    internal DocumentRange ToDocumentRange(ITreeNode node)
    {
      return ExecutionContext.GetDocumentRange(node);
    }

    public bool IsPreciseMode
    {
      get { return ExecutionContext.IsPreciseMode; }
    }

    [NotNull]
    public virtual PostfixExpressionContext FixExpression([NotNull] PostfixExpressionContext context)
    {
      return context;
    }
  }
}