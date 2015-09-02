using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Parsing;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.PostfixTemplates.Contexts.CSharp
{
  [Language(typeof(CSharpLanguage))]
  public class CSharpPostfixTemplateContextFactory : IPostfixTemplateContextFactory
  {
    public string[] GetReparseStrings()
    {
      return new[] {"__", "__;"};
    }

    public PostfixTemplateContext TryCreate(ITreeNode position, PostfixTemplateExecutionContext executionContext)
    {
      if (!(position is ICSharpIdentifier)) return null;

      // expr.__
      var referenceExpression = position.Parent as IReferenceExpression;
      if (referenceExpression != null && referenceExpression.Delimiter != null)
      {
        var qualifierExpression = referenceExpression.QualifierExpression;
        if (qualifierExpression != null)
        {
          return TryCreateFromReferenceExpression(executionContext, qualifierExpression, referenceExpression);
        }
      }

      // String.__
      var referenceName = position.Parent as IReferenceName;
      if (referenceName != null && referenceName.Qualifier != null && referenceName.Delimiter != null)
      {
        var typeUsage = referenceName.Parent as ITypeUsage;
        if (typeUsage != null)
        {
          var expression = typeUsage.Parent as ICSharpExpression;
          if (expression != null)
          {
            return new CSharpReferenceNamePostfixTemplateContext(referenceName, expression, executionContext);
          }
        }
      }

      // string.__
      var brokenStatement = FindBrokenStatement(position);
      if (brokenStatement != null)
      {
        var expressionStatement = brokenStatement.PrevSibling as IExpressionStatement;
        if (expressionStatement != null)
        {
          var expression = FindExpressionBrokenByKeyword(expressionStatement);
          if (expression != null)
          {
            return new CSharpBrokenStatementPostfixTemplateContext(brokenStatement, expression, executionContext);
          }
        }
      }

      return null;
    }

    [CanBeNull]
    private static PostfixTemplateContext TryCreateFromReferenceExpression(
      [NotNull] PostfixTemplateExecutionContext executionContext, [NotNull] ICSharpExpression qualifierExpression, [NotNull] IReferenceExpression referenceExpression)
    {
      // protect from 'o.M(.var)'
      var invocation = qualifierExpression as IInvocationExpression;
      if (invocation != null && invocation.LPar != null && invocation.RPar == null)
      {
        var argument = invocation.Arguments.LastOrDefault();
        if (argument != null && argument.Expression == null) return null;
      }

      // protect from 'smth.var\n(someCode).InBraces()'
      invocation = referenceExpression.Parent as IInvocationExpression;
      if (invocation != null)
      {
        for (ITokenNode lpar = invocation.LPar,
          token = invocation.InvokedExpression.NextSibling as ITokenNode;
          token != null && token != lpar && token.IsFiltered();
          token = token.NextSibling as ITokenNode)
        {
          if (token.GetTokenType() == CSharpTokenType.NEW_LINE) return null;
        }
      }

      // protect from 'doubleDot..var'
      var qualifierReference = qualifierExpression as IReferenceExpression;
      if (qualifierReference != null && qualifierReference.NameIdentifier == null) return null;

      return new CSharpReferenceExpressionPostfixTemplateContext(referenceExpression, qualifierExpression, executionContext);
    }

    [CanBeNull]
    private static ICSharpStatement FindBrokenStatement([NotNull] ITreeNode node)
    {
      var parent = node.Parent as ICSharpStatement;
      if (parent != null && parent.FirstChild == node && node.NextSibling is IErrorElement)
      {
        return parent;
      }

      var refExpr = node.Parent as IReferenceExpression;
      if (refExpr != null && refExpr.FirstChild == node)
      {
        if (refExpr.NextSibling is IErrorElement || !refExpr.IsPhysical())
        {
          return refExpr.Parent as IExpressionStatement;
        }
      }

      return null;
    }

    [CanBeNull]
    private static ICSharpExpression FindExpressionBrokenByKeyword([NotNull] IExpressionStatement statement)
    {
      ICSharpExpression expression = null;
      var errorFound = false;

      for (ITreeNode treeNode = statement, last; ; treeNode = last)
      {
        last = treeNode.LastChild; // inspect all the last nodes traversing up tree
        if (last == null) break;

        if (!errorFound) errorFound = last is IErrorElement;
        if (errorFound && last.Parent is ITypeUsage)
        {
          expression = last.Parent.Parent as ICSharpExpression;
          break;
        }

        // skip "expression statement is not closed with ';'" and friends
        while (last is IErrorElement) last = last.PrevSibling;

        if (last == null) break;
      }

      return expression;
    }
  }
}