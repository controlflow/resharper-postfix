using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion
{
  // todo: disable Foo(123.{here})

  public sealed class PostfixTemplateAcceptanceContext
  {
    [NotNull] private readonly ICSharpExpression myMostInnerExpression;
    [CanBeNull] private readonly ReparsedCodeCompletionContext myReparsedContext;

    public PostfixTemplateAcceptanceContext([NotNull] ITreeNode reference,
      [NotNull] ICSharpExpression expression, DocumentRange replaceRange,
      bool forceMode, [NotNull] PostfixExecutionContext context)
    {
      myReparsedContext = context.ReparsedContext;
      myMostInnerExpression = expression;
      PostfixReferenceNode = reference;
      ForceMode = forceMode;
      PsiModule = context.PsiModule;
      LookupItemsOwner = context.LookupItemsOwner;
      MostInnerReplaceRange = replaceRange;

      if (!replaceRange.IsValid())
      {
        var referenceExpression = reference as IReferenceExpression;
        if (referenceExpression != null)
        {
          MostInnerReplaceRange =
            ToDocumentRange(referenceExpression.QualifierExpression.NotNull()).SetEndTo(
              ToDocumentRange(referenceExpression.Delimiter.NotNull()).TextRange.EndOffset);
        }

        var referenceName = reference as IReferenceName;
        if (referenceName != null)
        {
          MostInnerReplaceRange =
            ToDocumentRange(referenceName.Qualifier).SetEndTo(
              ToDocumentRange(referenceName.Delimiter).TextRange.EndOffset);
        }
      }

      // build expression contexts
      var expressionContexts = new List<PrefixExpressionContext>();
      var endOffset = Math.Max(
        MostInnerReplaceRange.TextRange.EndOffset,
        ToDocumentRange(reference).TextRange.EndOffset);

      for (ITreeNode node = expression; node != null; node = node.Parent)
      {
        if (node is ICSharpStatement) break;

        var expr = node as ICSharpExpression;
        if (expr == null || expr == reference) continue;

        var exprRange = myReparsedContext.ToDocumentRange(expr);
        if (exprRange.TextRange.EndOffset > endOffset)
          break; // stop when 'a.var + b'

        var expressionContext = new PrefixExpressionContext(this, expr);
        expressionContexts.Add(expressionContext);
        if (expressionContext.CanBeStatement) break;
      }

      Expressions = expressionContexts.AsReadOnly();
      InnerExpression = expressionContexts[0];
      OuterExpression = expressionContexts[expressionContexts.Count - 1];
    }

    // Expression contexts: 'a', 'a + b.Length', '(a + b.Length)', '(a + b.Length) > 0.var'
    [NotNull] public IEnumerable<PrefixExpressionContext> Expressions { get; private set; }
    [NotNull] public PrefixExpressionContext InnerExpression { get; private set; }
    [NotNull] public PrefixExpressionContext OuterExpression { get; private set; }

    // Double basic completion (or basic completion in R# 7.1)
    public bool ForceMode { get; private set; }

    // Can be IReferenceExpression / IReferenceName / IErrorElement
    [NotNull] public ITreeNode PostfixReferenceNode { get; private set; }

    // Minimal replace range 'string.if' of 'o as string.if'
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

    [NotNull] public ILookupItemsOwner LookupItemsOwner { get; private set; }
    [NotNull] public IPsiModule PsiModule { get; private set; }

    internal DocumentRange ToDocumentRange(ITreeNode node)
    {
      return myReparsedContext.ToDocumentRange(node);
    }
  }
}