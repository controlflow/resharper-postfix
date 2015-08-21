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
    [CanBeNull] private IList<CSharpPostfixExpressionContext> myExpressions;
    [CanBeNull] private CSharpPostfixExpressionContext myTypeExpression;

    protected CSharpPostfixTemplateContext(
      [NotNull] ITreeNode reference, [NotNull] ICSharpExpression expression, [NotNull] PostfixTemplateExecutionContext executionContext)
      : base(reference, executionContext)
    {
      myInnerExpression = expression;
    }

    [NotNull] // todo: do we need it?
    public IEnumerable<CSharpPostfixExpressionContext> ExpressionsOrTypes
    {
      get
      {
        var expressions = Expressions; // force build
        if (myTypeExpression == null) return expressions;

        return expressions.Prepend(myTypeExpression);
      }
    }

    [NotNull, ItemNotNull]
    public IList<CSharpPostfixExpressionContext> Expressions
    {
      get
      {
        GC.KeepAlive(AllExpressions); // todo: hate this

        return myExpressions ?? EmptyList<CSharpPostfixExpressionContext>.InstanceList;
      }
    }

    protected override IList<PostfixExpressionContext> BuildAllExpressions()
    {
      var reference = Reference;

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

          myTypeExpression = expressionContext; // yeah, time to stop
          myExpressions = EmptyList<CSharpPostfixExpressionContext>.InstanceList;
          return EmptyList<PostfixExpressionContext>.InstanceList;
        }

        expressionContexts.Add(expressionContext);

        if (expressionContext.CanBeStatement) break;
      }

      myExpressions = expressionContexts.AsReadOnly();
      return expressionContexts.ConvertAll(x => (PostfixExpressionContext) x).AsReadOnly();
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