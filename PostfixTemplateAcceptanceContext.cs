using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.LiveTemplates.CSharp.Context;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion
{
  // todo: move from ranges to psi
  // todo: move from single expression to IEnumerable of containing expressions+types
  // todo: NodesToReplace?

  public sealed class PostfixTemplateAcceptanceContext
  {
    public PostfixTemplateAcceptanceContext(
      [NotNull] IReferenceExpression referenceExpression,
      [NotNull] ICSharpExpression expression,
      TextRange replaceRange, TextRange expressionRange,
      bool canBeStatement, bool looseChecks)
    {
      ReferenceExpression = referenceExpression;
      Expression = expression;
      ExpressionType = expression.Type();
      ReplaceRange = replaceRange;
      ExpressionRange = expressionRange;
      CanBeStatement = canBeStatement;
      LooseChecks = looseChecks;

      var expressionReference = expression as IReferenceExpression;
      if (expressionReference != null)
      {
        ReferencedElement = expressionReference.Reference.Resolve().DeclaredElement;
      }
      else
      {
        var typeExpression = expression as IPredefinedTypeExpression;
        if (typeExpression != null)
        {
          var typeName = typeExpression.PredefinedTypeName;
          if (typeName != null)
            ReferencedElement = typeName.Reference.Resolve().DeclaredElement;
        }
      }
    }

    public IEnumerable<PrefixExpression> PossibleExpressions
    {
      get
      {
        if (CanBeStatement)
        {
          yield return new PrefixExpression(Expression, true);
        }
        else
        {
          ITreeNode node = Expression;
          while (node != null)
          {
            var expr = node as ICSharpExpression;
            if (expr != null)
              yield return new PrefixExpression(
                expr, node != Expression && ExpressionStatementNavigator.GetByExpression(expr) != null);

            if (node is ICSharpStatement) break;
            node = node.Parent;
          }
        }
      }
    }

    [NotNull] public IReferenceExpression ReferenceExpression { get; private set; } // "lines.Any().if"
    [NotNull] public ICSharpExpression Expression { get; private set; } // "lines.Any()"
    [NotNull] public IType ExpressionType { get; private set; } // boolean
    [CanBeNull] public IDeclaredElement ReferencedElement { get; set; } // lines: LocalVar
    public TextRange ReplaceRange { get; set; } // todo: remove
    public TextRange ExpressionRange { get; set; } // todo: remove
    public bool CanBeStatement { get; private set; }
    public bool LooseChecks { get; private set; } // rename

    [CanBeNull] public ICSharpFunctionDeclaration ContainingFunction
    {
      get { return Expression.GetContainingNode<ICSharpFunctionDeclaration>(); }
    }
  }

  public sealed class PrefixExpression
  {
    public PrefixExpression([NotNull] ICSharpExpression expression, bool canBeStatement)
    {
      Expression = expression;
      ExpressionType = expression.Type(); // todo: maybe use NULL to indicate that expression is broken and types do not works
      CanBeStatement = canBeStatement;

      var expressionReference = expression as IReferenceExpression;
      if (expressionReference != null)
      {
        ReferencedElement = expressionReference.Reference.Resolve().DeclaredElement;
      }
      else
      {
        var typeExpression = expression as IPredefinedTypeExpression;
        if (typeExpression != null)
        {
          var typeName = typeExpression.PredefinedTypeName;
          if (typeName != null)
            ReferencedElement = typeName.Reference.Resolve().DeclaredElement;
        }
      }
    }

    [NotNull] public ICSharpExpression Expression { get; private set; } // "lines.Any()"
    [NotNull] public IType ExpressionType { get; private set; } // boolean
    [CanBeNull] public IDeclaredElement ReferencedElement { get; set; } // lines: LocalVar
    public bool CanBeStatement { get; private set; }
  }
}