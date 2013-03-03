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
      [NotNull] ICSharpExpression expression, [NotNull] IType expressionType,
      TextRange replaceRange, TextRange expressionRange,
      bool canBeStatement, bool looseChecks)
    {
      ReferenceExpression = referenceExpression;
      Expression = expression;
      ExpressionType = expressionType;
      ReplaceRange = replaceRange;
      ExpressionRange = expressionRange;
      CanBeStatement = canBeStatement;
      LooseChecks = looseChecks;
    }

    [NotNull] public IReferenceExpression ReferenceExpression { get; private set; } // "lines.Any().if"
    [NotNull] public ICSharpExpression Expression { get; private set; } // "lines.Any()"
    [NotNull] public IType ExpressionType { get; private set; } // boolean
    public TextRange ReplaceRange { get; set; }
    public TextRange ExpressionRange { get; set; }
    public bool CanBeStatement { get; private set; }
    public bool LooseChecks { get; private set; }

    [CanBeNull]
    public ICSharpFunctionDeclaration ContainingFunction
    {
      get { return Expression.GetContainingNode<ICSharpFunctionDeclaration>(); }
    }
  }
}