using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.PostfixTemplates.Contexts
{
  public abstract class PostfixTemplateContext
  {
    protected PostfixTemplateContext([NotNull] ITreeNode reference, [NotNull] PostfixExecutionContext executionContext)
    {
      Reference = reference;
      PsiModule = reference.GetPsiModule();
      ExecutionContext = executionContext;
    }

    [NotNull] public ITreeNode Reference { get; private set; }
    [NotNull] public IPsiModule PsiModule { get; private set; }

    [NotNull] public PostfixExecutionContext ExecutionContext { get; private set; }

    public abstract bool HasExpressions { get; }

    internal DocumentRange ToDocumentRange(ITreeNode node)
    {
      return ExecutionContext.GetDocumentRange(node);
    }

    public bool IsPreciseMode
    {
      get { return ExecutionContext.IsPreciseMode; }
    }
  }

  public abstract class PostfixTemplateContext<TPostfixExpressionContext> : PostfixTemplateContext
    where TPostfixExpressionContext : PostfixExpressionContext
  {
    [CanBeNull] private IList<TPostfixExpressionContext> myExpressions;

    protected PostfixTemplateContext([NotNull] ITreeNode reference, [NotNull] PostfixExecutionContext executionContext)
      : base(reference, executionContext) { }

    [NotNull, ItemNotNull]
    protected abstract IList<TPostfixExpressionContext> BuildExpressions();

    // Expressions: 'a', 'a + b.Length', '(a + b.Length)', '(a + b.Length) > 0.var'
    [NotNull] public IList<TPostfixExpressionContext> Expressions
    {
      get { return myExpressions ?? (myExpressions = BuildExpressions()); }
    }

    [NotNull]
    public virtual TPostfixExpressionContext FixExpression([NotNull] TPostfixExpressionContext context)
    {
      return context;
    }
  }
}