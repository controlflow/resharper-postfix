
using JetBrains.Annotations;
using JetBrains.Application.Progress;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
#if RESHARPER8
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi.Services;
#elif RESHARPER9
using JetBrains.DocumentManagers.Transactions;
using JetBrains.ReSharper.Feature.Services.Util;
#endif

namespace JetBrains.ReSharper.PostfixTemplates
{
  public class BrokenStatementPostfixTemplateContext : PostfixTemplateContext
  {
    public BrokenStatementPostfixTemplateContext([NotNull] ITreeNode reference,
                                                 [NotNull] ICSharpExpression expression,
                                                 [NotNull] PostfixExecutionContext executionContext)
      : base(reference, expression, executionContext) { }

    private static readonly string FixCommandName =
      typeof(BrokenStatementPostfixTemplateContext) + ".FixExpression";

    public override PrefixExpressionContext FixExpression(PrefixExpressionContext context)
    {
      var psiServices = Reference.GetPsiServices();
      var expressionRange = ExecutionContext.GetDocumentRange(context.Expression);
      var referenceRange = ExecutionContext.GetDocumentRange(Reference);

      var textWithReference = expressionRange.SetEndTo(referenceRange.TextRange.EndOffset).GetText();

      var indexOfReferenceDot = textWithReference.LastIndexOf('.');
      if (indexOfReferenceDot <= 0) return context;

      var realReferenceRange = referenceRange.SetStartTo(
        expressionRange.TextRange.StartOffset + indexOfReferenceDot);

      var document = expressionRange.Document;

      using (psiServices.Solution.CreateTransactionCookie(
        DefaultAction.Commit, FixCommandName, NullProgressIndicator.Instance))
      {
        document.ReplaceText(realReferenceRange.TextRange, ")");
        document.InsertText(expressionRange.TextRange.StartOffset, "unchecked(");
      }

      psiServices.CommitAllDocuments();

      var uncheckedExpression = TextControlToPsi.GetElement<IUncheckedExpression>(
        psiServices.Solution, document, expressionRange.TextRange.StartOffset + 1);

      if (uncheckedExpression == null) return context;

      var operand = uncheckedExpression.Operand;
      psiServices.DoTransaction(FixCommandName, () =>
      {
        LowLevelModificationUtil.DeleteChild(operand);
        LowLevelModificationUtil.ReplaceChildRange(
          uncheckedExpression, uncheckedExpression, operand);
      });

      Assertion.Assert(operand.IsPhysical(), "operand.IsPhysical()");

      return new PrefixExpressionContext(this, operand);
    }
  }
}