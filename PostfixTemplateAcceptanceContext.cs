using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion
{
  public sealed class PostfixTemplateAcceptanceContext
  {
    private readonly ReparsedCodeCompletionContext myReparsedContext;

    public PostfixTemplateAcceptanceContext(
      [NotNull] IReferenceExpression referenceExpression,
      [NotNull] ICSharpExpression expression,
      DocumentRange replaceRange,
      [CanBeNull] ReparsedCodeCompletionContext context,
      bool canBeStatement, bool forceMode)
    {
      myReparsedContext = context;

      PostfixReferenceExpression = referenceExpression;
      MostInnerExpression = expression;
      MostInnerReplaceRange = replaceRange.IsValid() ? replaceRange : ToDocumentRange(referenceExpression);

      CanBeStatement = canBeStatement; // better to remove
      ForceMode = forceMode;
      SettingsStore = expression.GetSettingsStore();
    }

    public DocumentRange ToDocumentRange([NotNull] ITreeNode treeNode)
    {
      var documentRange = treeNode.GetDocumentRange();
      if (myReparsedContext == null) return documentRange;

      var originalRange = myReparsedContext.ToDocumentRange(treeNode.GetTreeTextRange());
      return new DocumentRange(documentRange.Document, originalRange);
    }

    public IContextBoundSettingsStore SettingsStore { get; private set; }

    public IEnumerable<PrefixExpressionContext> PossibleExpressions
    {
      get
      {
        if (CanBeStatement)
        {
          yield return new PrefixExpressionContext(this, MostInnerExpression, true);
          yield break;
        }

        var leftRange = PostfixReferenceExpression.GetTreeEndOffset();
        ITreeNode node = MostInnerExpression;
        while (node != null)
        {
          var expr = node as ICSharpExpression;
          if (expr != null)
          {
            if (expr.GetTreeEndOffset() > leftRange) break;

            yield return new PrefixExpressionContext(this,
              expr, node != MostInnerExpression && ExpressionStatementNavigator.GetByExpression(expr) != null);
          }

          if (node is ICSharpStatement) break;
          node = node.Parent;
        }
      }
    }

    [NotNull] public IReferenceExpression PostfixReferenceExpression { get; private set; }
    [NotNull] public ICSharpExpression MostInnerExpression { get; private set; }

    public DocumentRange MostInnerReplaceRange { get; private set; }
    public DocumentRange PostfixReferenceRange
    {
      get { return ToDocumentRange(PostfixReferenceExpression); }
    }

    // todo: remove from here?
    public bool CanBeStatement { get; private set; }
    public bool ForceMode { get; private set; }

    [CanBeNull] public ICSharpFunctionDeclaration ContainingFunction
    {
      get { return MostInnerExpression.GetContainingNode<ICSharpFunctionDeclaration>(); }
    }
  }
}