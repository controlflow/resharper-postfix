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
    [NotNull] private readonly ICSharpExpression myMostInnerExpression;
    [CanBeNull] private IList<PrefixExpressionContext> myExpressions;

    public PostfixTemplateContext(
      [NotNull] ITreeNode reference, [NotNull] ICSharpExpression expression,
      [NotNull] PostfixExecutionContext executionContext)
    {
      myMostInnerExpression = expression;
      PostfixReferenceNode = reference;


      //MostInnerReplaceRange = replaceRange;
      ExecutionContext = executionContext;

      //if (!replaceRange.IsValid())
      //{
      //  var referenceExpression = reference as IReferenceExpression;
      //  if (referenceExpression != null)
      //  {
      //    var qualifier = referenceExpression.QualifierExpression.NotNull();
      //    var delimiter = referenceExpression.Delimiter.NotNull();
      //    MostInnerReplaceRange = ToDocumentRange(qualifier)
      //      .SetEndTo(ToDocumentRange(delimiter).TextRange.EndOffset);
      //  }
      //
      //  var referenceName = reference as IReferenceName;
      //  if (referenceName != null)
      //  {
      //    var qualifier = referenceName.Qualifier;
      //    var delimiter = referenceName.Delimiter;
      //    MostInnerReplaceRange = ToDocumentRange(qualifier)
      //      .SetEndTo(ToDocumentRange(delimiter).TextRange.EndOffset);
      //  }
      //}
      
    }

    [NotNull] private IList<PrefixExpressionContext> BuildExpression()
    {
      var reference = PostfixReferenceNode;

      // build expression contexts
      var expressionContexts = new List<PrefixExpressionContext>();
      var endOffset = Math.Max(
        //MostInnerReplaceRange.TextRange.EndOffset,
        0,
        ToDocumentRange(reference).TextRange.EndOffset);

      for (ITreeNode node = myMostInnerExpression; node != null; node = node.Parent)
      {
        if (node is ICSharpStatement) break;

        var expression = node as ICSharpExpression;
        if (expression == null || expression == reference) continue;

        var expressionRange = ExecutionContext.GetDocumentRange(expression);
        if (!expressionRange.IsValid())
          break; // stop when out of generated
        if (expressionRange.TextRange.EndOffset > endOffset)
          break; // stop when 'a.var + b'

        // skip relational expressions like this: 'List<int.{here}>'
        if (CommonUtils.IsRelationalExpressionWithTypeOperand(expression)) continue;

        var expressionContext = new PrefixExpressionContext(this, expression);
        if (expressionContext.ReferencedElement is ITypeElement)
        {
          // skip types that are parts of 'List<T.>'-like expressions
          if (!CommonUtils.CanTypeBecameExpression(myMostInnerExpression)) continue;
        }

        expressionContexts.Add(expressionContext);
        if (expressionContext.CanBeStatement) break;
      }

      return expressionContexts.AsReadOnly();
    }

    // Expression contexts: 'a', 'a + b.Length', '(a + b.Length)', '(a + b.Length) > 0.var'
    [NotNull] public IList<PrefixExpressionContext> Expressions
    {
      get { return myExpressions ?? (myExpressions = BuildExpression()); }
    }

    [NotNull] public PrefixExpressionContext InnerExpression
    {
      get { return Expressions[0]; }
    }

    [NotNull] public PrefixExpressionContext OuterExpression
    {
      get { return Expressions[Expressions.Count - 1]; }
    }

    [NotNull] public PostfixExecutionContext ExecutionContext { get; private set; }

    // Double basic completion (or basic completion in R# 7.1)
    public bool IsForceMode
    {
      get { return ExecutionContext.IsForceMode; }
    }

    // Can be IReferenceExpression / IReferenceName / IErrorElement
    [NotNull] public ITreeNode PostfixReferenceNode { get; private set; }

    // Minimal replace range 'string.if' of 'o as string.if'
    [Obsolete("get rid of it")]
    public DocumentRange MostInnerReplaceRange { get; private set; }

    // Minimal reference-like/error node range
    public DocumentRange PostfixReferenceRange
    {
      get { return ToDocumentRange(PostfixReferenceNode); }
    }

    [CanBeNull] public ICSharpFunctionDeclaration ContainingFunction
    {
      get { return myMostInnerExpression.GetContainingNode<ICSharpFunctionDeclaration>(); }
    }

    internal DocumentRange ToDocumentRange(ITreeNode node)
    {
      return ExecutionContext.GetDocumentRange(node);
    }
  }
}