using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion
{
  // todo: disable Foo(123.{here})

  public sealed class PostfixTemplateAcceptanceContext
  {
    private readonly ReparsedCodeCompletionContext myReparsedContext;

    public PostfixTemplateAcceptanceContext(
      [NotNull] IReferenceExpression reference,
      [NotNull] ICSharpExpression expression, DocumentRange replaceRange,
      [CanBeNull] ReparsedCodeCompletionContext context, bool forceMode)
    {
      myReparsedContext = context;

      PostfixReferenceExpression = reference;
      MostInnerExpression = expression;

      // todo: don't like it
      MostInnerReplaceRange = replaceRange.IsValid()
        ? replaceRange
        : ToDocumentRange(reference.QualifierExpression)
           .SetEndTo(ToDocumentRange(reference.Delimiter).TextRange.EndOffset);

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
        for (ITreeNode node = MostInnerExpression; node != null; node = node.Parent)
        {
          var expr = node as ICSharpExpression;
          if (expr != null)
          {
            if (PostfixReferenceExpression == expr) continue;
            yield return new PrefixExpressionContext(this, expr);
          }

          if (node is ICSharpStatement) break;
        }
      }
    }

    [NotNull] public IReferenceExpression PostfixReferenceExpression { get; private set; }

    // todo: review usages
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