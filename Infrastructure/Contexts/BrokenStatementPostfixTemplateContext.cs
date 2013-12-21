using JetBrains.Annotations;
using JetBrains.Application.Progress;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Services;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion
{
  public class BrokenStatementPostfixTemplateContext : PostfixTemplateContext
  {
    public BrokenStatementPostfixTemplateContext(
      [NotNull] ITreeNode reference, [NotNull] ICSharpExpression expression,
      [NotNull] PostfixExecutionContext executionContext)
      : base(reference, expression, executionContext) { }

    private static readonly string FixCommandName =
      typeof(BrokenStatementPostfixTemplateContext) + ".FixExpression";

    public override PrefixExpressionContext FixExpression(PrefixExpressionContext context)
    {
      var expressionRange = ExecutionContext.GetDocumentRange(context.Expression);
      var referenceRange = ExecutionContext.GetDocumentRange(Reference);

      var text = expressionRange.SetEndTo(referenceRange.TextRange.EndOffset).GetText();
      var indexOfReferenceDot = text.LastIndexOf('.');
      if (indexOfReferenceDot <= 0) return context;

      var realReferenceRange = referenceRange.SetStartTo(
        expressionRange.TextRange.StartOffset + indexOfReferenceDot);

      var solution = ExecutionContext.PsiModule.GetSolution();
      var document = context.Expression.GetDocumentRange().Document;

      using (solution.CreateTransactionCookie(
        DefaultAction.Commit, FixCommandName, NullProgressIndicator.Instance))
      {
        document.ReplaceText(realReferenceRange.TextRange, ")");
        document.InsertText(expressionRange.TextRange.StartOffset, "unchecked(");
      }

      solution.GetPsiServices().CommitAllDocuments();

      var uncheckedExpression = TextControlToPsi.GetElement<IUncheckedExpression>(
        solution, document, expressionRange.TextRange.StartOffset + 1);
      if (uncheckedExpression != null)
      {
        var operand = uncheckedExpression.Operand;
        solution.GetPsiServices().DoTransaction(FixCommandName, () =>
        {
          LowLevelModificationUtil.DeleteChild(operand);
          LowLevelModificationUtil.ReplaceChildRange(
            uncheckedExpression, uncheckedExpression, operand);
        });

        Assertion.Assert(operand.IsPhysical(), "operand.IsPhysical()");
        return new PrefixExpressionContext(this, operand);
      }

      return context;
    }
  }
}