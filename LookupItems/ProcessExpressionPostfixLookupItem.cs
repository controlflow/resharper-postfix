using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Services;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems
{
  //public abstract class ProcessExpressionPostfixLookupItem : PostfixStatementLookupItem<IExpressionStatement>
  //{
  //  protected ProcessExpressionPostfixLookupItem(
  //    [NotNull] string shortcut, [NotNull] PrefixExpressionContext context)
  //    : base(shortcut, context) { }
  //
  //
  //
  //  protected sealed override void AfterCompletion(
  //    ITextControl textControl, ISolution solution, Suffix suffix,
  //    TextRange resultRange, string targetText, int caretOffset)
  //  {
  //    solution.GetPsiServices().CommitAllDocuments();
  //
  //    var expressions = TextControlToPsi
  //      .GetSelectedElements<ICSharpExpression>(solution, textControl.Document, resultRange);
  //    foreach (var expression in expressions)
  //    {
  //      AcceptExpression(textControl, solution, resultRange, expression);
  //      break;
  //    }
  //  }
  //
  //  protected abstract void AcceptExpression(
  //    [NotNull] ITextControl textControl, [NotNull] ISolution solution,
  //    TextRange resultRange, [NotNull] ICSharpExpression expression);
  //}
}