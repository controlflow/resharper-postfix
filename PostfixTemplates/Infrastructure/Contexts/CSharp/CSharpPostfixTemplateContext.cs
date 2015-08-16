using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.PostfixTemplates.Contexts.CSharp
{
  public class CSharpPostfixTemplateContext : PostfixTemplateContext<CSharpPostfixExpressionContext>
  {
    [NotNull] private readonly ICSharpExpression myInnerExpression;
    [CanBeNull] private CSharpPostfixExpressionContext myTypeExpression;

    protected CSharpPostfixTemplateContext(
      [NotNull] ITreeNode reference, [NotNull] ICSharpExpression expression, [NotNull] PostfixExecutionContext executionContext)
      : base(reference, executionContext)
    {
      myInnerExpression = expression;
    }

    [NotNull]
    public IEnumerable<PostfixExpressionContext> ExpressionsOrTypes
    {
      get
      {
        var expressions = Expressions; // force build
        if (myTypeExpression == null) return expressions;

        return expressions.Prepend(myTypeExpression);
      }
    }

    protected override IList<CSharpPostfixExpressionContext> BuildExpressions()
    {
      var reference = Reference;

      // build expression contexts
      var contexts = new List<CSharpPostfixExpressionContext>();
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

        var expressionContext = new CSharpPostfixExpressionContext(this, expression);
        if (expressionContext.ReferencedElement is ITypeElement)
        {
          // skip types that are parts of 'List<T.>'-like expressions
          if (!CommonUtils.CanTypeBecameExpression(myInnerExpression)) continue;
          if (myTypeExpression != null) break; // should never happens

          myTypeExpression = expressionContext;
          return EmptyList<CSharpPostfixExpressionContext>.InstanceList; // yeah, time to stop
        }

        contexts.Add(expressionContext);
        if (expressionContext.CanBeStatement) break;
      }

      return contexts.AsReadOnly();
    }

    // Most inner expression: '0.var'
    [CanBeNull] public PostfixExpressionContext InnerExpression
    {
      get
      {
        var contexts = Expressions;
        return (contexts.Count == 0) ? null : contexts[0];
      }
    }

    // Most outer expression: '(a + b.Length) > 0.var'
    [CanBeNull] public CSharpPostfixExpressionContext OuterExpression
    {
      get
      {
        var contexts = Expressions;
        return (contexts.Count == 0) ? null : contexts[contexts.Count - 1];
      }
    }

    [CanBeNull] public CSharpPostfixExpressionContext TypeExpression
    {
      get { return myTypeExpression; }
    }

    [CanBeNull] public ICSharpFunctionDeclaration ContainingFunction
    {
      get { return myInnerExpression.GetContainingNode<ICSharpFunctionDeclaration>(); }
    }

    [NotNull] public virtual ICSharpExpression GetOuterExpression([NotNull] ICSharpExpression expression)
    {
      return expression;
    }
  }
}