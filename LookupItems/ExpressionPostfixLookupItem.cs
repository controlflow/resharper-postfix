using System;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Services;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;

#if RESHARPER8
using JetBrains.ReSharper.Psi.Modules;
#endif

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems
{
  public abstract class ExpressionPostfixLookupItem<TExpression> : PostfixLookupItem
    where TExpression : class, ICSharpExpression
  {
    protected ExpressionPostfixLookupItem(
      [NotNull] string shortcut, [NotNull] PrefixExpressionContext context)
      : base(shortcut, context) { }

    protected override void ExpandPostfix(
      ITextControl textControl, Suffix suffix, ISolution solution,
      DocumentRange replaceRange, IPsiModule psiModule, ICSharpExpression expression)
    {
      textControl.Document.ReplaceText(replaceRange.TextRange, PostfixMarker);
      solution.GetPsiServices().CommitAllDocuments();

      int? caretPosition = null;
      TExpression newExpression = null;
      using (WriteLockCookie.Create())
      {
        var commandName = GetType().FullName + " expansion";
        solution.GetPsiServices().DoTransaction(commandName, () =>
        {
          var referenceExpressions = TextControlToPsi.GetElements<IReferenceExpression>(
            solution, textControl.Document, replaceRange.TextRange.StartOffset);

          foreach (var reference in referenceExpressions)
          {
            if (!IsMarkerExpression(reference, PostfixMarker)) continue;

            //expression.SetResolveContextForSandBox(reference);
            // TODO: TODO
            if (!expression.IsPhysical())
            {
              expression.SetResolveContextForSandBox(reference);
            }

            var factory = CSharpElementFactory.GetInstance(psiModule);
            newExpression = CreateExpression(factory, expression);

            // find caret marker in created expression
            var caretMarker = new TreeNodeMarker(Guid.NewGuid().ToString());
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

            caretMarker.Unmark(newExpression);
            break;
          }
        });
      }

      if (newExpression != null)
        AfterComplete(textControl, suffix, newExpression, caretPosition);
    }

    protected virtual void AfterComplete(
      [NotNull] ITextControl textControl, [NotNull] Suffix suffix,
      [NotNull] TExpression expression, int? caretPosition)
    {
      AfterComplete(textControl, suffix, caretPosition);
    }

    [NotNull] protected abstract TExpression CreateExpression(
      [NotNull] CSharpElementFactory factory, [NotNull] ICSharpExpression expression);

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