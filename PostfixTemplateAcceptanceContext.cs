using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion
{
  // todo: remove obsolete shit

  public sealed class PostfixTemplateAcceptanceContext
  {
    public PostfixTemplateAcceptanceContext(
      [NotNull] IReferenceExpression referenceExpression,
      [NotNull] ICSharpExpression expression,
      TextRange replaceRange, TextRange expressionRange,
      bool canBeStatement, bool forceMode)
    {
      ReferenceExpression = referenceExpression;
      Expression = expression;
      ExpressionType = expression.Type();
      MinimalReplaceRange = replaceRange;
      ExpressionRange = expressionRange;
      CanBeStatement = canBeStatement;
      ForceMode = forceMode;
      SettingsStore = expression.GetSettingsStore();

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

    public IContextBoundSettingsStore SettingsStore { get; private set; }

    public IEnumerable<PrefixExpressionContext> PossibleExpressions
    {
      get
      {
        if (CanBeStatement)
        {
          yield return new PrefixExpressionContext(
            Expression, true,
            ReferenceExpression, MinimalReplaceRange);
        }
        else
        {
          var leftRange = ReferenceExpression.GetTreeEndOffset();
          ITreeNode node = Expression;
          while (node != null)
          {
            // todo: check expression range

            var expr = node as ICSharpExpression;
            if (expr != null)
            {
              if (expr.GetTreeEndOffset() > leftRange) break;

              yield return new PrefixExpressionContext(
                expr, node != Expression && ExpressionStatementNavigator.GetByExpression(expr) != null,
                ReferenceExpression, MinimalReplaceRange);
            }

            if (node is ICSharpStatement) break;
            node = node.Parent;
          }
        }
      }
    }

    [Obsolete] [NotNull] public IReferenceExpression ReferenceExpression { get; private set; } // "lines.Any().if"
    [Obsolete] [NotNull] public ICSharpExpression Expression { get; private set; } // "lines.Any()"
    [Obsolete] [NotNull] public IType ExpressionType { get; private set; } // boolean
    [Obsolete] [CanBeNull] public IDeclaredElement ReferencedElement { get; set; } // lines: LocalVar
    [Obsolete] public TextRange MinimalReplaceRange { get; set; }
    [Obsolete] public TextRange ExpressionRange { get; set; }
    [Obsolete] public bool CanBeStatement { get; private set; }
    public bool ForceMode { get; private set; } // rename

    [CanBeNull] public ICSharpFunctionDeclaration ContainingFunction
    {
      get { return Expression.GetContainingNode<ICSharpFunctionDeclaration>(); }
    }
  }
}