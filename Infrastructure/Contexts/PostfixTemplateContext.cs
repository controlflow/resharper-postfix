using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.PostfixTemplates
{
  public class PostfixTemplateContext
  {
    [NotNull] private readonly ICSharpExpression myInnerExpression;
    [CanBeNull] private IList<PrefixExpressionContext> myAllExpressions, myExpressions;

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
      var prevStartOffset = -1;

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
        if (expressionRange.TextRange.StartOffset == prevStartOffset)
          break; // track start offset is changes when we are going up

        prevStartOffset = expressionRange.TextRange.StartOffset;

        // skip relational expressions like this: 'List<int.{here}>'
        if (CommonUtils.IsRelationalExpressionWithTypeOperand(expression))
          continue;

        var expressionContext = new PrefixExpressionContext(this, expression);
        if (expressionContext.ReferencedElement is ITypeElement)
        {
          // skip types that are parts of 'List<T.>'-like expressions
          if (!CommonUtils.CanTypeBecameExpression(myInnerExpression))
            continue;
        }

        contexts.Add(expressionContext);
        if (expressionContext.CanBeStatement) break;
      }

      return contexts.AsReadOnly();
    }

    // Expressions: 'a', 'a + b.Length', '(a + b.Length)', '(a + b.Length) > 0.var'
    [NotNull] public IList<PrefixExpressionContext> Expressions
    {
      get
      {
        if (myExpressions != null) return myExpressions;

        var contexts = new List<PrefixExpressionContext>();
        foreach (var context in ExpressionsOrTypes)
          if (context.ReferencedType == null) contexts.Add(context);

        return myExpressions = contexts;
      }
    }

    [NotNull] public IList<PrefixExpressionContext> ExpressionsOrTypes
    {
      get { return myAllExpressions ?? (myAllExpressions = BuildExpressions()); }
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