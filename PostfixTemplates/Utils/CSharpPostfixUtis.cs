using System;
using JetBrains.Annotations;
using JetBrains.Metadata.Reader.Impl;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.PostfixTemplates.Contexts.CSharp;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Parsing;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.PostfixTemplates
{
  public static class CSharpPostfixUtis
  {
    public static bool IsForeachEnumeratorPatternType([NotNull] this ITypeElement typeElement)
    {
      return CSharpDeclaredElementUtil.IsForeachEnumeratorPatternType(typeElement);
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
          return !(IsRelationalExpressionWithTypeOperand(previous.FirstChild) &&
                   previous.FirstChild != null &&
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

    public static bool IsValidExpressionWithValue([NotNull] ICSharpExpression expression)
    {
      if (expression is IAnonymousFunctionExpression) return false;

      if (expression is IInvocationExpression)
      {
        var expressionType = expression.GetExpressionType();
        if (expressionType.ToIType().IsVoid()) return false;
      }

      var literalExpression = expression as ILiteralExpression;
      if (literalExpression != null)
      {
        var literalToken = literalExpression.Literal;
        if (literalToken != null)
        {
          if (literalToken.GetTokenType() == CSharpTokenType.NULL_KEYWORD) return false;
        }
      }

      return true;
    }

    [NotNull]
    public static CSharpPostfixExpressionContext[] FindExpressionWithValuesContexts(
      [NotNull] CSharpPostfixTemplateContext context, [CanBeNull] Predicate<ICSharpExpression> predicate = null)
    {
      var results = new LocalList<CSharpPostfixExpressionContext>();

      foreach (var expressionContext in context.Expressions.Reverse())
      {
        if (IsValidExpressionWithValue(expressionContext.Expression))
        {
          if (predicate == null || predicate(expressionContext.Expression))
          {
            results.Add(expressionContext);
          }
        }
      }

      return results.ToArray();
    }

    [CanBeNull]
    public static IReferenceExpression ToReferenceExpression([CanBeNull] this ReparsedCodeCompletionContext context)
    {
      if (context == null) return null;

      var reference = context.Reference as IReferenceExpressionReference;
      if (reference == null) return null;

      return reference.GetTreeNode() as IReferenceExpression;
    }

    // todo: [C#6] what about conditional access?
    public static bool IsReferenceExpressionsChain([CanBeNull] ICSharpExpression expression)
    {
      do
      {
        var referenceExpression = expression as IReferenceExpression;
        if (referenceExpression == null) return false;

        expression = referenceExpression.QualifierExpression;
      }
      while (expression != null);

      return true;
    }

    [NotNull]
    private static readonly ClrTypeName ConfigurableAwaitable = new ClrTypeName("System.Runtime.CompilerServices.ConfiguredTaskAwaitable");

    [Pure, ContractAnnotation("null => false")]
    public static bool IsConfigurableAwaitable(this IType type)
    {
      var declaredType = type as IDeclaredType;
      return declaredType != null && declaredType.GetClrName().Equals(ConfigurableAwaitable);
    }

    [NotNull]
    private static readonly ClrTypeName GenericConfigurableAwaitable = new ClrTypeName("System.Runtime.CompilerServices.ConfiguredTaskAwaitable`1");

    [Pure, ContractAnnotation("null => false")]
    public static bool IsGenericConfigurableAwaitable(this IType type)
    {
      var declaredType = type as IDeclaredType;
      return declaredType != null && declaredType.GetClrName().Equals(GenericConfigurableAwaitable);
    }
  }
}