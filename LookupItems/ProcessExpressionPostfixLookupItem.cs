using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Services;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems
{
  public abstract class ProcessExpressionPostfixLookupItem : PostfixLookupItem
  {
    protected ProcessExpressionPostfixLookupItem(
      [NotNull] PostfixTemplateAcceptanceContext context, [NotNull] string shortcut)
      : base(context, shortcut, "$EXPR$") { }

    protected sealed override void AfterCompletion(
      ITextControl textControl, ISolution solution, Suffix suffix,
      TextRange resultRange, string targetText, int caretOffset)
    {
      solution.GetPsiServices().PsiManager.CommitAllDocuments();

      var expressions = TextControlToPsi
        .GetSelectedElements<ICSharpExpression>(solution, textControl.Document, resultRange);
      foreach (var expression in expressions)
      {
        AcceptExpression(textControl, solution, resultRange, expression);
        break;
      }
    }

    protected abstract void AcceptExpression(
      [NotNull] ITextControl textControl, [NotNull] ISolution solution,
      TextRange resultRange, [NotNull] ICSharpExpression expression);
  }
}