using JetBrains.Annotations;
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Razor.Impl.CustomHandlers;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ProjectModel.FileTypes;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion
{
  public sealed class PrefixExpressionContext
  {
    public PrefixExpressionContext(
      [NotNull] PostfixTemplateContext parent, [NotNull] ICSharpExpression expression)
    {
      Parent = parent;
      Expression = expression;
      Type = expression.Type();
      CanBeStatement = CalculateCanBeStatement(expression);

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

    private static bool CalculateCanBeStatement([NotNull] ICSharpExpression expression)
    {
      if (ExpressionStatementNavigator.GetByExpression(expression) != null)
        return true;

      // Razor support
      var argument = CSharpArgumentNavigator.GetByValue(
        ReferenceExpressionNavigator.GetByQualifierExpression(expression));
      if (argument != null && argument.Kind == ParameterKind.VALUE)
      {
        var invocation = InvocationExpressionNavigator.GetByArgument(argument);
        if (invocation != null)
        {
          var referenceExpression = invocation.InvokedExpression as IReferenceExpression;
          if (referenceExpression != null && referenceExpression.QualifierExpression == null)
          {
            var services = argument.GetSolution().GetComponent<IProjectFileTypeServices>();
            var sourceFile = argument.GetSourceFile();
            if (sourceFile != null)
            {
              var razorService = services.TryGetService<IRazorPsiServices>(sourceFile.LanguageType);
              if (razorService != null && razorService.IsSpecialMethodInvocation(invocation, RazorMethodType.Write))
                return true;
            }
          }
        }
      }

      // handle broken trees like: "lines.     \r\n   NextLineStatemement();"
      var containingStatement = expression.GetContainingNode<ICSharpStatement>();
      if (containingStatement != null)
      {
        var expressionOffset = expression.GetTreeStartOffset();
        var statementOffset = containingStatement.GetTreeStartOffset();
        return (expressionOffset == statementOffset);
      }

      return false;
    }

    [NotNull] public PostfixTemplateContext Parent { get; private set; }

    // "lines.Any()" : Boolean
    [NotNull] public ICSharpExpression Expression { get; private set; }
    [NotNull] public IType Type { get; private set; }

    [CanBeNull] public IDeclaredElement ReferencedElement { get; private set; }
    [CanBeNull] public IDeclaredType ReferencedType { get; private set; }

    public bool CanBeStatement { get; private set; }

    public DocumentRange ExpressionRange
    {
      get { return Parent.ToDocumentRange(Expression); }
    }
  }
}