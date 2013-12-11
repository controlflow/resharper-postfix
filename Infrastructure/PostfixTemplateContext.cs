using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion
{
  public sealed class PostfixTemplateContext
  {
    [NotNull] private readonly ITreeNode myReference;
    [NotNull] private readonly PostfixExecutionContext myExecutionContext;
    [NotNull] private readonly ICSharpExpression myInnerExpression;
    [CanBeNull] private IList<PrefixExpressionContext> myExpressions;

    public PostfixTemplateContext(
      [NotNull] ITreeNode reference, [NotNull] ICSharpExpression expression,
      [NotNull] PostfixExecutionContext executionContext)
    {
      myReference = reference;
      myInnerExpression = expression;
      myExecutionContext = executionContext;
    }

    [NotNull] private IList<PrefixExpressionContext> BuildExpression()
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
        if (expression == null || expression == reference) continue;

        var expressionRange = ExecutionContext.GetDocumentRange(expression);
        if (!expressionRange.IsValid())
          break; // stop when out of generated
        if (expressionRange.TextRange.EndOffset > endOffset)
          break; // stop when 'a.var + b'
        if (expressionRange.TextRange.StartOffset == prevStartOffset)
          break; // track start offset is changes when we are going up

        prevStartOffset = expressionRange.TextRange.StartOffset;

        // skip relational expressions like this: 'List<int.{here}>'
        if (CommonUtils.IsRelationalExpressionWithTypeOperand(expression)) continue;

        var expressionContext = new PrefixExpressionContext(this, expression);
        if (expressionContext.ReferencedElement is ITypeElement)
        {
          // skip types that are parts of 'List<T.>'-like expressions
          if (!CommonUtils.CanTypeBecameExpression(myInnerExpression)) continue;
        }

        contexts.Add(expressionContext);
        if (expressionContext.CanBeStatement) break;
      }

      return contexts.AsReadOnly();
    }

    // Expressions: 'a', 'a + b.Length', '(a + b.Length)', '(a + b.Length) > 0.var'
    [NotNull] public IList<PrefixExpressionContext> Expressions
    {
      get { return myExpressions ?? (myExpressions = BuildExpression()); }
    }

    // Most inner expression: '0.var'
    [NotNull] public PrefixExpressionContext InnerExpression
    {
      get { return Expressions[0]; }
    }

    // Most outer expression: '(a + b.Length) > 0.var'
    [NotNull] public PrefixExpressionContext OuterExpression
    {
      get { return Expressions[Expressions.Count - 1]; }
    }

    [NotNull] public PostfixExecutionContext ExecutionContext
    {
      get { return myExecutionContext; }
    }

    [NotNull] public ITreeNode Reference
    {
      get { return myReference; }
    }

    public bool IsForceMode
    {
      get { return ExecutionContext.IsForceMode; }
    }

    [CanBeNull] public ICSharpFunctionDeclaration ContainingFunction
    {
      get { return myInnerExpression.GetContainingNode<ICSharpFunctionDeclaration>(); }
    }

    internal DocumentRange ToDocumentRange(ITreeNode node)
    {
      return ExecutionContext.GetDocumentRange(node);
    }

    // todo: temporary, prettify this shit...
    public Func<PostfixTemplateContext, PrefixExpressionContext, PrefixExpressionContext> Fix =
      (context, expressionContext) => expressionContext;

    public PrefixExpressionContext FixExpression(PrefixExpressionContext executionContext)
    {
      return executionContext;
    }
  }
}