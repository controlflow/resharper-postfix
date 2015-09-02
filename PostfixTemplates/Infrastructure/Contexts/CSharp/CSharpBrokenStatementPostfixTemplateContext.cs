using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.Util;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.PostfixTemplates.Contexts.CSharp
{
  public class CSharpBrokenStatementPostfixTemplateContext : CSharpPostfixTemplateContext
  {
    public CSharpBrokenStatementPostfixTemplateContext(
      [NotNull] ITreeNode reference, [NotNull] ICSharpExpression expression, [NotNull] PostfixTemplateExecutionContext executionContext)
      : base(reference, expression, executionContext) { }

    private static readonly string FixCommandName =
      typeof(CSharpBrokenStatementPostfixTemplateContext) + ".FixExpression";

    public override CSharpPostfixExpressionContext FixExpression(CSharpPostfixExpressionContext context)
    {
      var psiServices = Reference.GetPsiServices();
      var expressionRange = ExecutionContext.GetDocumentRange(context.Expression);
      var referenceRange = ExecutionContext.GetDocumentRange(Reference);

      var textWithReference = expressionRange.SetEndTo(referenceRange.TextRange.EndOffset).GetText();

      var indexOfReferenceDot = textWithReference.LastIndexOf('.');
      if (indexOfReferenceDot <= 0) return context;

      var realReferenceRange = referenceRange.SetStartTo(expressionRange.TextRange.StartOffset + indexOfReferenceDot);

      var transactionManager = psiServices.Transactions.DocumentTransactionManager;
      var document = expressionRange.Document;

      // todo: make sure this is not in undo stack!
      using (transactionManager.CreateTransactionCookie(DefaultAction.Commit, FixCommandName))
      {
        document.ReplaceText(realReferenceRange.TextRange, ")");
        document.InsertText(expressionRange.TextRange.StartOffset, "unchecked(");
      }

      //using (psiServices.Solution.CreateTransactionCookie(DefaultAction.Commit, FixCommandName, NullProgressIndicator.Instance))
      //{
      //  
      //}

      psiServices.Files.CommitAllDocuments();

      var uncheckedExpression = TextControlToPsi.GetElement<IUncheckedExpression>(psiServices.Solution, document, expressionRange.TextRange.StartOffset + 1);
      if (uncheckedExpression == null) return context;

      var operand = uncheckedExpression.Operand;
      psiServices.Transactions.Execute(FixCommandName, () =>
      {
        LowLevelModificationUtil.DeleteChild(operand);
        LowLevelModificationUtil.ReplaceChildRange(uncheckedExpression, uncheckedExpression, operand);
      });

      Assertion.Assert(operand.IsPhysical(), "operand.IsPhysical()");

      return new CSharpPostfixExpressionContext(this, operand);
    }
  }
}