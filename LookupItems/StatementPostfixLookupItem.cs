using System;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Services;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;
using JetBrains.Util;
#if RESHARPER8
using JetBrains.ReSharper.Psi.Modules;
#endif

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems
{
  public abstract class StatementPostfixLookupItem<TStatement> : PostfixLookupItem
    where TStatement : class, ICSharpStatement
  {
    protected StatementPostfixLookupItem(
      [NotNull] string shortcut, [NotNull] PrefixExpressionContext context)
      : base(shortcut, context) { }

    protected override void ExpandPostfix(
      ITextControl textControl, Suffix suffix, ISolution solution, TextRange replaceRange,
      IPsiModule psiModule, ICSharpExpression expression)
    {
      textControl.Document.ReplaceText(replaceRange, PostfixMarker + ";");
      solution.GetPsiServices().CommitAllDocuments();

      int? caretPosition = null;
      TStatement newStatement = null;
      using (WriteLockCookie.Create())
      {
        var commandName = GetType().FullName + " expansion";
        solution.GetPsiServices().DoTransaction(commandName, () =>
        {
          var expressionStatements = TextControlToPsi.GetElements<IExpressionStatement>(
            solution, textControl.Document, replaceRange.StartOffset);

          foreach (var statement in expressionStatements)
          {
            if (!IsMarkerExpressionStatement(statement, PostfixMarker)) continue;

            var factory = CSharpElementFactory.GetInstance(psiModule);
            newStatement = CreateStatement(psiModule, factory);

            // find caret marker in created statement
            var caretMarker = new TreeNodeMarker(Guid.NewGuid().ToString());
            var collector = new RecursiveElementCollector<IExpressionStatement>(
              expressionStatement => IsMarkerExpressionStatement(expressionStatement, CaretMarker));
            var caretNodes = collector.ProcessElement(newStatement).GetResults();
            if (caretNodes.Count == 1) caretMarker.Mark(caretNodes[0]);

            // replace marker statement with the new one
            newStatement = statement.ReplaceBy(newStatement);
            PlaceExpression(newStatement, expression, factory);

            // find and remove caret marker node
            var caretNode = caretMarker.FindMarkedNode(newStatement);
            if (caretNode != null)
            {
              caretPosition = caretNode.GetDocumentRange().TextRange.StartOffset;
              LowLevelModificationUtil.DeleteChild(caretNode);
            }

            caretMarker.Unmark(newStatement);
            break;
          }
        });
      }

      AfterComplete(textControl, suffix, newStatement, caretPosition);
    }

    protected virtual void AfterComplete(
      [NotNull] ITextControl textControl, [NotNull] Suffix suffix,
      [CanBeNull] TStatement newStatement, int? caretPosition)
    {
      AfterComplete(textControl, suffix, caretPosition);
    }

    [NotNull] protected abstract TStatement CreateStatement(
      [NotNull] IPsiModule psiModule, [NotNull] CSharpElementFactory factory);

    protected abstract void PlaceExpression(
      [NotNull] TStatement statement, [NotNull] ICSharpExpression expression,
      [NotNull] CSharpElementFactory factory);

    private static bool IsMarkerExpressionStatement(
      [NotNull] IExpressionStatement expressionStatement, [NotNull] string markerName)
    {
      var reference = expressionStatement.Expression as IReferenceExpression;
      return reference != null
          && reference.QualifierExpression == null
          && reference.Delimiter == null
          && reference.NameIdentifier.Name == markerName;
    }
  }
}