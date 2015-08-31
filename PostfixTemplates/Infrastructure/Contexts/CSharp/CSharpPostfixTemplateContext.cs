using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.PostfixTemplates.Contexts.CSharp
{
  public class CSharpPostfixTemplateContext : PostfixTemplateContext
  {
    [NotNull] private readonly ICSharpExpression myInnerExpression;
    [NotNull] private readonly IList<CSharpPostfixExpressionContext> myExpressions;
    [CanBeNull] private readonly CSharpPostfixExpressionContext myTypeExpression;

    protected CSharpPostfixTemplateContext(
      [NotNull] ITreeNode reference, [NotNull] ICSharpExpression expression, [NotNull] PostfixTemplateExecutionContext executionContext)
      : base(reference, executionContext)
    {
      myInnerExpression = expression;
      myExpressions = BuildExpressions(Reference, out myTypeExpression);
    }

    [NotNull] // todo: do we need it?
    public IEnumerable<CSharpPostfixExpressionContext> ExpressionsOrTypes
    {
      get
      {
        var expressions = Expressions; // force build

        return (myTypeExpression != null)
          ? expressions.Prepend(myTypeExpression)
          : expressions;
      }
    }

    [NotNull, ItemNotNull]
    public IList<CSharpPostfixExpressionContext> Expressions
    {
      get { return myExpressions; }
    }

    [NotNull]
    private IList<CSharpPostfixExpressionContext> BuildExpressions(
      [NotNull] ITreeNode reference, [CanBeNull] out CSharpPostfixExpressionContext typeContext)
    {
      // build expression contexts
      var expressionContexts = new List<CSharpPostfixExpressionContext>();
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
        if (CSharpPostfixUtis.IsRelationalExpressionWithTypeOperand(expression))
          continue;

        var expressionContext = new CSharpPostfixExpressionContext(this, expression);
        if (expressionContext.ReferencedElement is ITypeElement)
        {
          // skip types that are parts of 'List<T.>'-like expressions
          if (!CSharpPostfixUtis.CanTypeBecameExpression(myInnerExpression)) continue;
          if (myTypeExpression != null) break; // should never happens

          typeContext = expressionContext; // yeah, time to stop
          return EmptyList<CSharpPostfixExpressionContext>.InstanceList;
        }

        expressionContexts.Add(expressionContext);

        if (expressionContext.CanBeStatement) break;
      }

      typeContext = null;
      return expressionContexts.AsReadOnly();
    }

    protected override IEnumerable<PostfixExpressionContext> GetAllExpressionContexts()
    {
      foreach (var expressionContext in Expressions)
        yield return expressionContext;

      if (TypeExpression != null)
        yield return TypeExpression;
    }

    // Most inner expression: '0.var'
    [CanBeNull] public CSharpPostfixExpressionContext InnerExpression
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

    // todo: review usages and drop
    [CanBeNull] public ICSharpFunctionDeclaration ContainingFunction
    {
      get { return myInnerExpression.GetContainingNode<ICSharpFunctionDeclaration>(); }
    }

    public override PostfixExpressionContext FixExpression(PostfixExpressionContext context)
    {
      return FixExpression((CSharpPostfixExpressionContext) context);
    }

    [NotNull]
    public virtual CSharpPostfixExpressionContext FixExpression([NotNull] CSharpPostfixExpressionContext context)
    {
      return context;
    }

    [NotNull] public virtual ICSharpExpression GetOuterExpression([NotNull] ICSharpExpression expression)
    {
      return expression;
    }
  }
}