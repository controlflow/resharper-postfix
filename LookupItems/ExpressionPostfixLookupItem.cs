using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.LinqTools;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Services;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems
{
  public abstract class ExpressionPostfixLookupItem<TExpression> : PostfixLookupItem
    where TExpression : class, ICSharpExpression
  {
    protected ExpressionPostfixLookupItem(
      [NotNull] string shortcut, [NotNull] PrefixExpressionContext context)
      : base(shortcut, context) { }

    protected override void ExpandPostfix(
      ITextControl textControl, Suffix suffix, ISolution solution, TextRange replaceRange,
      IPsiModule psiModule, ICSharpExpression expression)
    {
      textControl.Document.ReplaceText(replaceRange, PostfixMarker);
      solution.GetPsiServices().CommitAllDocuments();

      int? caretPosition = null;
      TExpression newExpression = null;
      using (WriteLockCookie.Create())
      {
        var commandName = GetType().FullName + " expansion";
        var transactions = solution.GetPsiServices().Transactions;
        transactions.Execute(commandName, () =>
        {
          var referenceExpressions = TextControlToPsi.GetElements<IReferenceExpression>(
            solution, textControl.Document, replaceRange.StartOffset);

          foreach (var reference in referenceExpressions)
          {
            if (!IsMarkerExpression(reference, PostfixMarker)) continue;

            var factory = CSharpElementFactory.GetInstance(psiModule);
            newExpression = CreateExpression(psiModule, factory, expression);

            // find caret marker in created expression
            var caretMarker = new TreeNodeMarker<IReferenceExpression>();
            var collector = new RecursiveElementCollector<IReferenceExpression>(
              expressionStatement => IsMarkerExpression(expressionStatement, CaretMarker));
            var caretNodes = collector.ProcessElement(newExpression).GetResults();
            if (caretNodes.Count == 1) caretMarker.Mark(caretNodes[0]);

            // replace marker expression with the new one
            newExpression = reference.ReplaceBy(newExpression);

            // find and remove caret marker node
            var caretNode = caretMarker.FindMarkedNode(newExpression);
            if (caretNode != null)
            {
              caretPosition = caretNode.GetDocumentRange().TextRange.StartOffset;
              LowLevelModificationUtil.DeleteChild(caretNode);
            }

            caretMarker.Dispose(newExpression);
            break;
          }
        });
      }

      AfterComplete(textControl, suffix, newExpression, caretPosition);
    }

    protected virtual void AfterComplete(
      [NotNull] ITextControl textControl, [NotNull] Suffix suffix,
      [CanBeNull] TExpression expression, int? caretPosition)
    {
      AfterComplete(textControl, suffix, caretPosition);
    }

    [NotNull] protected abstract TExpression CreateExpression(
      [NotNull] IPsiModule psiModule, [NotNull] CSharpElementFactory factory,
      [NotNull] ICSharpExpression expression);

    private static bool IsMarkerExpression(
      [NotNull] ICSharpExpression expression, [NotNull] string markerName)
    {
      var reference = expression as IReferenceExpression;
      return reference != null
          && reference.QualifierExpression == null
          && reference.Delimiter == null
          && reference.NameIdentifier.Name == markerName;
    }
  }
}