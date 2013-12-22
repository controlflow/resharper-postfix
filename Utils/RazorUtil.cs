using JetBrains.Annotations;
using JetBrains.Application.Progress;
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.FileTypes;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Razor.Impl.CustomHandlers;
using JetBrains.ReSharper.Psi.Razor.Tree;
using JetBrains.ReSharper.Psi.Services;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.PostfixTemplates
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
        {
          return ExpressionStatementNavigator.GetByExpression(invocation);
        }
      }

      return null;
    }

    [CanBeNull]
    public static ICSharpStatement FixExpressionToStatement(
      DocumentRange expressionRange, [NotNull] IPsiServices psiServices)
    {
      var solution = psiServices.Solution;
      var expressionOffset = expressionRange.TextRange.StartOffset;
      var document = expressionRange.Document;

      foreach (var razorExpression in
        TextControlToPsi.GetElements<IRazorImplicitExpression>(solution, document, expressionOffset))
      {
        var razorRange = razorExpression.GetDocumentRange();

        const string commandName = "Replacing razor expression with statement";
        using (solution.CreateTransactionCookie(
          DefaultAction.Commit, commandName, NullProgressIndicator.Instance))
        {
          razorRange.Document.ReplaceText(razorRange.TextRange, "@using(null){}");
        }

        solution.GetPsiServices().CommitAllDocuments();

        foreach (var razorStatement in
          TextControlToPsi.GetElements<IUsingStatement>(solution, document, expressionOffset))
        {
          return razorStatement;
        }

        break;
      }

      return null;
    }
  }
}