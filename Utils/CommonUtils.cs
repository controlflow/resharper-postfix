using JetBrains.Annotations;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Parsing;
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

    [CanBeNull] public static ITokenNode FindSemicolonAfter(
      [NotNull] ICSharpExpression expression, [NotNull] ITreeNode reference)
    {
      var statement = ExpressionStatementNavigator.GetByExpression(expression);
      if (statement == null)
      {
        var referenceExpression = ReferenceExpressionNavigator.GetByQualifierExpression(expression);
        if (referenceExpression != null && referenceExpression == reference)
          statement = ExpressionStatementNavigator.GetByExpression(referenceExpression);

        if (statement == null) return null;
      }

      return statement.Semicolon;
    }

    public static bool CanTypeBecameExpression([CanBeNull] ICSharpExpression expression)
    {
      var referenceExpression = expression as IReferenceExpression;
      if (referenceExpression != null)
      {
        var parent = referenceExpression.Parent;

        if (IsRelationalExpressionWithTypeOperand(parent)) return false;

        var expressionStatement = parent as IExpressionStatement;
        if (expressionStatement != null)
        {
          var previous = expressionStatement.GetPreviousStatementInBlock();
          if (previous == null) return true;

          // two children: relational and error element
          return !(
            IsRelationalExpressionWithTypeOperand(previous.FirstChild) &&
            previous.FirstChild.NextSibling == previous.LastChild &&
            previous.LastChild is IErrorElement);
        }

        return CanTypeBecameExpression(parent as IReferenceExpression);
      }

      var predefinedType = expression as IPredefinedTypeExpression;
      if (predefinedType != null)
      {
        var parent = predefinedType.Parent;

        return CanTypeBecameExpression(parent as IReferenceExpression);
      }

      return true;
    }

    [ContractAnnotation("null => false")]
    public static bool IsRelationalExpressionWithTypeOperand([CanBeNull] ITreeNode node)
    {
      var relationalExpression = node as IRelationalExpression;
      if (relationalExpression == null) return false;

      var operatorSign = relationalExpression.OperatorSign;
      if (operatorSign != null && operatorSign.GetTokenType() == CSharpTokenType.LT)
      {
        var left = relationalExpression.LeftOperand as IReferenceExpression;
        if (left != null && left.Reference.Resolve().DeclaredElement is ITypeElement)
          return true;

        var right = relationalExpression.LeftOperand as IReferenceExpression;
        if (right != null && right.Reference.Resolve().DeclaredElement is ITypeElement)
          return true;
      }

      return false;
    }

    public static bool IsNiceExpression([NotNull] ICSharpExpression expression)
    {
      if (expression is IAssignmentExpression) return false;
      if (expression is IPrefixOperatorExpression) return false;
      if (expression is IPostfixOperatorExpression) return false;

      if (expression is IInvocationExpression)
      {
        var expressionType = expression.GetExpressionType();
        if (expressionType.ToIType().IsVoid()) return false;
      }

      return true;
    }
  }
}