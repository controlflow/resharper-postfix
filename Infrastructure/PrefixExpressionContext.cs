using JetBrains.Annotations;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.PostfixTemplates
{
  public sealed class PrefixExpressionContext
  {
    public PrefixExpressionContext([NotNull] PostfixTemplateContext postfixContext,
                                   [NotNull] ICSharpExpression expression)
    {
      PostfixContext = postfixContext;
      Expression = expression;
      CanBeStatement = GetContainingStatement() != null;

      var brokenType = IsBrokenAsExpressionCase(expression, postfixContext.Reference);
      ExpressionType = brokenType ?? expression.GetExpressionType();
      Type = brokenType ?? expression.Type();

      var referenceExpression = expression as IReferenceExpression;
      if (referenceExpression != null)
      {
        var resolveResult = referenceExpression.Reference.Resolve().Result;
        ReferencedElement = resolveResult.DeclaredElement;

        var element = ReferencedElement as ITypeElement;
        if (element != null)
          ReferencedType = TypeFactory.CreateType(element, resolveResult.Substitution);
      }

      var predefinedTypeExpression = expression as IPredefinedTypeExpression;
      if (predefinedTypeExpression != null)
      {
        var typeName = predefinedTypeExpression.PredefinedTypeName;
        if (typeName != null)
        {
          var resolveResult = typeName.Reference.Resolve().Result;
          ReferencedElement = resolveResult.DeclaredElement;

          var element = ReferencedElement as ITypeElement;
          if (element != null)
            ReferencedType = TypeFactory.CreateType(element, resolveResult.Substitution);
        }
      }
    }

    [CanBeNull] public ICSharpStatement GetContainingStatement()
    {
      var expression = PostfixContext.GetOuterExpression(Expression);

      ICSharpStatement statement = ExpressionStatementNavigator.GetByExpression(expression);
      if (statement != null) return statement;

      statement = RazorUtil.CanBeStatement(expression);
      if (statement != null) return statement;

      return null;
    }

    [NotNull] public PostfixTemplateContext PostfixContext { get; private set; }

    // "lines.Any()" : Boolean
    [NotNull] public ICSharpExpression Expression { get; private set; }
    [NotNull] public IExpressionType ExpressionType { get; private set; }
    [NotNull] public IType Type { get; private set; }

    [CanBeNull] public IDeclaredElement ReferencedElement { get; private set; }
    [CanBeNull] public IDeclaredType ReferencedType { get; private set; }

    public bool CanBeStatement { get; private set; }

    public DocumentRange ExpressionRange
    {
      get { return PostfixContext.ToDocumentRange(Expression); }
    }

    // A M(object o) { o as A.re| } - postfix reference breaks as-expression type
    [CanBeNull]
    private static IDeclaredType IsBrokenAsExpressionCase(
      [NotNull] ICSharpExpression expression, [NotNull] ITreeNode reference)
    {
      var asExpression = expression as IAsExpression;
      if (asExpression == null) return null;

      var userTypeUsage = asExpression.TypeOperand as IUserTypeUsage;
      if (userTypeUsage == null) return null;

      var referenceName = userTypeUsage.ScalarTypeName;
      if (referenceName != userTypeUsage.LastChild || referenceName != reference)
        return null;

      var qualifier = referenceName.Qualifier;
      if (qualifier == null) return null;

      var resolveResult = qualifier.Reference.Resolve().Result;
      var typeElement = resolveResult.DeclaredElement as ITypeElement;
      if (typeElement != null)
      {
        return TypeFactory.CreateType(typeElement, resolveResult.Substitution);
      }

      return null;
    }
  }
}