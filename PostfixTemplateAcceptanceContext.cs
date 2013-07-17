using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion
{
  // todo: move from ranges to psi
  // todo: move from single expression to IEnumerable of containing expressions+types
  // todo: NodesToReplace?

  public class PostfixTemplateAcceptanceContext
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
}