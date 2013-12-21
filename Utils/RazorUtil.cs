using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.FileTypes;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Razor.Impl.CustomHandlers;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion
{
  public static class RazorUtil
  {
    [CanBeNull]
    public static ICSharpStatement CanBeStatement([NotNull] ICSharpExpression expression)
    {
      var argument = CSharpArgumentNavigator.GetByValue(expression);
      if (argument == null || argument.Kind != ParameterKind.VALUE) return null;

      var invocation = InvocationExpressionNavigator.GetByArgument(argument);
      if (invocation == null) return null;

      var referenceExpression = invocation.InvokedExpression as IReferenceExpression;
      if (referenceExpression != null && referenceExpression.QualifierExpression == null)
      {
        var services = argument.GetSolution().GetComponent<IProjectFileTypeServices>();
        var sourceFile = argument.GetSourceFile();
        if (sourceFile == null) return null;

        var razorService = services.TryGetService<IRazorPsiServices>(sourceFile.LanguageType);
        if (razorService != null && razorService.IsSpecialMethodInvocation(invocation, RazorMethodType.Write))
          return ExpressionStatementNavigator.GetByExpression(invocation);
      }

      return null;
    }
  }
}