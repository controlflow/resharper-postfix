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

    [NotNull, ItemNotNull]
    public IList<CSharpPostfixExpressionContext> Expressions
    {
      get { return myExpressions; }
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

    // Usage of some resolved type: 'StringBuilder.new'
    [CanBeNull]
    public CSharpPostfixExpressionContext TypeExpression
    {
      get { return myTypeExpression; }
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

    public override bool IsSemanticallyMakeSence()
    {
      // todo: what about extern aliases?

      var innerExpression = InnerExpression; // shit happens
      if (innerExpression != null && innerExpression.ReferencedElement is INamespace)
      {
        return false;
      }

      return base.IsSemanticallyMakeSence();
    }

    // todo: review usages and drop
    [CanBeNull] public ICSharpFunctionDeclaration ContainingFunction
    {
      get { return myInnerExpression.GetContainingNode<ICSharpFunctionDeclaration>(); }
    }

    [CanBeNull]
    public ICSharpDeclaration ContainingReturnValueOwner
    {
      get
      {
        // todo: [R#] replace with type-relaxed M:JetBrains.ReSharper.Psi.CSharp.Util.ReturnStatementUtil.FindReturnOwnerDeclaration(JetBrains.ReSharper.Psi.CSharp.Tree.IReturnValueHolder)

        foreach (var containingNode in myInnerExpression.ContainingNodes<ICSharpDeclaration>())
        {
          if (containingNode is ICSharpFunctionDeclaration) return containingNode;
          if (containingNode is IExpressionBodyOwnerDeclaration) return containingNode;
          if (containingNode is IAnonymousFunctionExpression) return containingNode;
        }

        return null;
      }
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