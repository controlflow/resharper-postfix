using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion
{
  // todo: maybe use NULL to indicate that expression is broken and types do not works
  // todo: pass parent context
  // todo: calculate CanBeExpression?

  public sealed class PrefixExpressionContext
  {
    public PrefixExpressionContext(
      [NotNull] ICSharpExpression expression, bool canBeStatement,
      [NotNull] IReferenceExpression referenceExpression,
      TextRange replaceRange)
    {
      Expression = expression;
      Reference = referenceExpression;
      ExpressionType = expression.Type();
      CanBeStatement = canBeStatement;
      ReplaceRange = expression.GetDocumentRange().TextRange.JoinRight(replaceRange);

      var reference = expression as IReferenceExpression;
      if (reference != null)
      {
        ReferencedElement = reference.Reference.Resolve().DeclaredElement;
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
    [NotNull] public IReferenceExpression Reference { get; private set; } // "lines.Any().if"
    [NotNull] public IType ExpressionType { get; private set; } // Boolean
    [CanBeNull] public IDeclaredElement ReferencedElement { get; set; } // lines: LocalVar
    public bool CanBeStatement { get; private set; }
    public TextRange ReplaceRange { get; private set; }
  }
}