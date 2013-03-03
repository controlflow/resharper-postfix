using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion
{
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

      ContainingFunction = Expression.GetContainingNode<ICSharpFunctionDeclaration>();

      var expressionReference = expression as IReferenceExpression;
      if (expressionReference != null)
      {
        ExpressionReferencedElement = expressionReference.Reference.Resolve().DeclaredElement;
      }
      else
      {
        var typeExpression = expression as IPredefinedTypeExpression;
        if (typeExpression != null)
        {
          var typeName = typeExpression.PredefinedTypeName;
          if (typeName != null)
            ExpressionReferencedElement = typeName.Reference.Resolve().DeclaredElement;
        }
      }
    }

    [NotNull] public IReferenceExpression ReferenceExpression { get; private set; } // "lines.Any().if"
    [NotNull] public ICSharpExpression Expression { get; private set; } // "lines.Any()"
    [NotNull] public IType ExpressionType { get; private set; } // boolean
    [CanBeNull] public IDeclaredElement ExpressionReferencedElement { get; set; } // lines: LocalVar
    public TextRange ReplaceRange { get; set; }
    public TextRange ExpressionRange { get; set; }
    public bool CanBeStatement { get; private set; }
    public bool LooseChecks { get; private set; }

    [CanBeNull] public ICSharpFunctionDeclaration ContainingFunction { get; private set; }
  }
}