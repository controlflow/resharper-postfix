using JetBrains.Annotations;
using JetBrains.Application.Progress;
using JetBrains.DocumentManagers.Transactions;
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.FileTypes;
using JetBrains.ReSharper.Feature.Services.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Razor.Impl.CustomHandlers;
using JetBrains.ReSharper.Psi.Razor.Tree;
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

        var service = services.TryGetService<IRazorPsiServices>(sourceFile.LanguageType);
        if (service != null && service.IsSpecialMethodInvocation(invocation, RazorMethodType.Write))
        {
          return ExpressionStatementNavigator.GetByExpression(invocation);
        }
      }

      return null;
    }

    [CanBeNull]
    public static ICSharpStatement FixExpressionToStatement(DocumentRange expressionRange, [NotNull] IPsiServices psiServices)
    {
      var solution = psiServices.Solution;
      var offset = expressionRange.TextRange.StartOffset;
      var document = expressionRange.Document;

      var expressions = TextControlToPsi.GetElements<IRazorImplicitExpression>(solution, document, offset);
      foreach (var razorExpression in expressions)
      {
        var razorRange = razorExpression.GetDocumentRange();

        const string commandName = "Replacing razor expression with statement";
        using (solution.CreateTransactionCookie(
          DefaultAction.Commit, commandName, NullProgressIndicator.Instance))
        {
          razorRange.Document.ReplaceText(razorRange.TextRange, "@using(null){}");
        }

        solution.GetPsiServices().Files.CommitAllDocuments();

        var statements = TextControlToPsi.GetElements<IUsingStatement>(solution, document, offset);
        foreach (var razorStatement in statements)
        {
          return razorStatement;
        }

        break;
      }

      return null;
    }
  }
}