using JetBrains.Annotations;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion
{
  public static class CommonUtils
  {
    public static DocumentRange ToDocumentRange(
      [CanBeNull] this ReparsedCodeCompletionContext context, [NotNull] ITreeNode treeNode)
    {
      var documentRange = treeNode.GetDocumentRange();
      if (context == null) return documentRange;

      var originalRange = context.ToDocumentRange(treeNode.GetTreeTextRange());
      return new DocumentRange(documentRange.Document, originalRange);
    }

    public static bool HasSemicolonAfter([NotNull] this PrefixExpressionContext context)
    {
      var statement = ExpressionStatementNavigator.GetByExpression(context.Expression);
      if (statement == null)
      {
        var referenceExpression = ReferenceExpressionNavigator.GetByQualifierExpression(context.Expression);
        if (referenceExpression != null && referenceExpression == context.Parent.PostfixReferenceNode)
          statement = ExpressionStatementNavigator.GetByExpression(referenceExpression);

        if (statement == null) return false;
      }

      return (statement.Semicolon != null);
    }
  }
}