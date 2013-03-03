using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion
{
  public interface IPostfixTemplateProvider
  {
    // todo: different behavior for auto/basic completion?
    // todo: extract parameters to 'PostfixTemplateAcceptanceContext'

    [NotNull]
    IEnumerable<PostfixLookupItem> CreateItems([NotNull] PostfixTemplateAcceptanceContext context);
  }

  public class PostfixTemplateAcceptanceContext
  {
    public PostfixTemplateAcceptanceContext(
      [NotNull] IReferenceExpression referenceExpression, [NotNull] ICSharpExpression expression,
      [NotNull] IType expressionType, bool canBeStatement, bool looseChecks)
    {
      ReferenceExpression = referenceExpression;
      Expression = expression;
      ExpressionType = expressionType;
      CanBeStatement = canBeStatement;
      LooseChecks = looseChecks;
    }

    // todo: put expression/replace ranges here?
    // todo: immutable PostfixLookupItem?

    [NotNull] public IReferenceExpression ReferenceExpression { get; private set; } // "lines.Any().if"
    [NotNull] public ICSharpExpression Expression { get; private set; } // "lines.Any()"
    [NotNull] public IType ExpressionType { get; private set; } // boolean
    public bool CanBeStatement { get; private set; }
    public bool LooseChecks { get; private set; }

    [CanBeNull]
    public ICSharpFunctionDeclaration ContainingFunction
    {
      get { return Expression.GetContainingNode<ICSharpFunctionDeclaration>(); }
    }
  }
}