using JetBrains.Annotations;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.PostfixTemplates.Contexts
{
  public abstract class PostfixExpressionContext
  {
    protected PostfixExpressionContext([NotNull] PostfixTemplateContext postfixContext, [NotNull] ITreeNode expression)
    {
      PostfixContext = postfixContext;
      Expression = expression;
    }

    [NotNull] public PostfixTemplateContext PostfixContext { get; private set; }

    [NotNull] public ITreeNode Expression { get; private set; }

    public DocumentRange ExpressionRange
    {
      get { return PostfixContext.ToDocumentRange(Expression); }
    }
  }
}