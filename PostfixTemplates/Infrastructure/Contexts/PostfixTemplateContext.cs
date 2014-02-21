using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.PostfixTemplates
{
  public class PostfixTemplateContext
  {
    [NotNull] private readonly ICSharpExpression myInnerExpression;
    [CanBeNull] private IList<PrefixExpressionContext> myExpressions;
    [CanBeNull] private PrefixExpressionContext myTypeExpression;

    protected PostfixTemplateContext([NotNull] ITreeNode reference,
                                     [NotNull] ICSharpExpression expression,
                                     [NotNull] PostfixExecutionContext executionContext)
    {
      myInnerExpression = expression;
      Reference = reference;
      PsiModule = reference.GetPsiModule();
      ExecutionContext = executionContext;
    }

    [NotNull] private IList<PrefixExpressionContext> BuildExpressions()
    {
      var reference = Reference;

      // build expression contexts
      var contexts = new List<PrefixExpressionContext>();
      var endOffset = ToDocumentRange(reference).TextRange.EndOffset;
      var previousStartOffset = -1;

      for (ITreeNode node = myInnerExpression; node != null; node = node.Parent)
      {
        if (node is ICSharpStatement) break;

        var expression = node as ICSharpExpression;
        if (expression == null || expression == reference)
          continue;

        var expressionRange = ExecutionContext.GetDocumentRange(expression);
        if (!expressionRange.IsValid())
          break; // stop when out of generated
        if (expressionRange.TextRange.EndOffset > endOffset)
          break; // stop when 'a.var + b'
        if (expressionRange.TextRange.StartOffset == previousStartOffset)
          break; // track start offset is changes when we are going up

        previousStartOffset = expressionRange.TextRange.StartOffset;

        // skip relational expressions like this: 'List<int.{here}>'
        if (CommonUtils.IsRelationalExpressionWithTypeOperand(expression))
          continue;

        var expressionContext = new PrefixExpressionContext(this, expression);
        if (expressionContext.ReferencedElement is ITypeElement)
        {
          // skip types that are parts of 'List<T.>'-like expressions
          if (!CommonUtils.CanTypeBecameExpression(myInnerExpression)) continue;
          if (myTypeExpression != null) break; // should never happens

          myTypeExpression = expressionContext;
          return EmptyList<PrefixExpressionContext>.InstanceList; // yeah, time to stop
        }

        contexts.Add(expressionContext);
        if (expressionContext.CanBeStatement) break;
      }

      return contexts.AsReadOnly();
    }

    // Expressions: 'a', 'a + b.Length', '(a + b.Length)', '(a + b.Length) > 0.var'
    [NotNull] public IList<PrefixExpressionContext> Expressions
    {
      get { return myExpressions ?? (myExpressions = BuildExpressions()); }
    }

    [NotNull] public IEnumerable<PrefixExpressionContext> ExpressionsOrTypes
    {
      get
      {
        var expressions = Expressions; // force build
        if (myTypeExpression == null) return expressions;

        return expressions.Prepend(myTypeExpression);
      }
    }

    // Most inner expression: '0.var'
    [CanBeNull] public PrefixExpressionContext InnerExpression
    {
      get
      {
        var contexts = Expressions;
        return (contexts.Count == 0) ? null : contexts[0];
      }
    }

    // Most outer expression: '(a + b.Length) > 0.var'
    [CanBeNull] public PrefixExpressionContext OuterExpression
    {
      get
      {
        var contexts = Expressions;
        return (contexts.Count == 0) ? null : contexts[contexts.Count - 1];
      }
    }

    [CanBeNull] public PrefixExpressionContext TypeExpression
    {
      get { return myTypeExpression; }
    }

    [NotNull] public ITreeNode Reference { get; private set; }

    [NotNull] public PostfixExecutionContext ExecutionContext { get; private set; }
    [NotNull] public IPsiModule PsiModule { get; private set; }

    public bool IsAutoCompletion
    {
      get { return ExecutionContext.IsAutoCompletion; }
    }

    [CanBeNull] public ICSharpFunctionDeclaration ContainingFunction
    {
      get { return myInnerExpression.GetContainingNode<ICSharpFunctionDeclaration>(); }
    }

    internal DocumentRange ToDocumentRange(ITreeNode node)
    {
      return ExecutionContext.GetDocumentRange(node);
    }

    [NotNull]
    public virtual PrefixExpressionContext FixExpression([NotNull] PrefixExpressionContext context)
    {
      return context;
    }

    [NotNull]
    public virtual ICSharpExpression GetOuterExpression([NotNull] ICSharpExpression expression)
    {
      return expression;
    }
  }
}