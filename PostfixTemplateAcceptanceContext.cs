using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion
{
  // todo: disable Foo(123.{here})

  public sealed class PostfixTemplateAcceptanceContext
  {
    [NotNull] private readonly ICSharpExpression myMostInnerExpression;
    [CanBeNull] private readonly ReparsedCodeCompletionContext myReparsedContext;

    public PostfixTemplateAcceptanceContext(
      [NotNull] IReferenceExpression reference,
      [NotNull] ICSharpExpression expression, DocumentRange replaceRange,
      [CanBeNull] ReparsedCodeCompletionContext context, bool forceMode)
    {
      myReparsedContext = context;
      myMostInnerExpression = expression;
      PostfixReferenceExpression = reference;
      ForceMode = forceMode;

      // todo: don't like it
      MostInnerReplaceRange = replaceRange.IsValid()
        ? replaceRange
        : ToDocumentRange(reference.QualifierExpression)
           .SetEndTo(ToDocumentRange(reference.Delimiter).TextRange.EndOffset);

      // build expression contexts
      var expressionContexts = new List<PrefixExpressionContext>();
      for (ITreeNode node = expression; node != null; node = node.Parent)
      {
        var expr = node as ICSharpExpression;
        if (expr != null)
        {
          if (PostfixReferenceExpression == expr) continue;
          var expressionContext = new PrefixExpressionContext(this, expr);
          expressionContexts.Add(expressionContext);
          if (expressionContext.CanBeStatement) break;
        }

        if (node is ICSharpStatement) break;
      }

      Expressions = expressionContexts.AsReadOnly();
      InnerExpression = expressionContexts[0];
      OuterExpression = expressionContexts[expressionContexts.Count - 1];
    }

    public DocumentRange ToDocumentRange([NotNull] ITreeNode treeNode)
    {
      var documentRange = treeNode.GetDocumentRange();
      if (myReparsedContext == null) return documentRange;

      var originalRange = myReparsedContext.ToDocumentRange(treeNode.GetTreeTextRange());
      return new DocumentRange(documentRange.Document, originalRange);
    }

    [NotNull] public IReferenceExpression PostfixReferenceExpression { get; private set; }

    [NotNull] public IEnumerable<PrefixExpressionContext> Expressions { get; private set; }
    [NotNull] public PrefixExpressionContext InnerExpression { get; private set; }
    [NotNull] public PrefixExpressionContext OuterExpression { get; private set; }

    public DocumentRange MostInnerReplaceRange { get; private set; }
    public DocumentRange PostfixReferenceRange
    {
      get { return ToDocumentRange(PostfixReferenceExpression); }
    }

    public bool ForceMode { get; private set; }

    [CanBeNull] public ICSharpFunctionDeclaration ContainingFunction
    {
      get { return myMostInnerExpression.GetContainingNode<ICSharpFunctionDeclaration>(); }
    }
  }
}