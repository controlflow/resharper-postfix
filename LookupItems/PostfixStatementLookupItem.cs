using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.Application.Settings;
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
  public abstract class PostfixStatementLookupItem<TStatement> : PostfixLookupItem
    where TStatement : class, ICSharpStatement
  {
    protected PostfixStatementLookupItem([NotNull] string shortcut,
      [NotNull] PostfixTemplateAcceptanceContext context,
      [NotNull] PrefixExpressionContext expression)
      : base(shortcut, context, expression) { }

    protected override void ExpandPostfix(
      ITextControl textControl, Suffix suffix, ISolution solution, TextRange replaceRange,
      IPsiModule psiModule, IContextBoundSettingsStore settings, ICSharpExpression expression)
    {
      textControl.Document.ReplaceText(replaceRange, PostfixMarker + ";");
      solution.GetPsiServices().CommitAllDocuments();

      int? caretPosition = null;
      using (WriteLockCookie.Create())
      {
        var commandName = GetType().FullName + " expansion";
        solution.GetPsiServices().Transactions.Execute(commandName, () =>
        {
          var expressionStatements = TextControlToPsi.GetElements<IExpressionStatement>(
            solution, textControl.Document, replaceRange.StartOffset);

          foreach (var statement in expressionStatements)
          {
            if (!IsMarkerExpressionStatement(statement, PostfixMarker)) continue;

            var factory = CSharpElementFactory.GetInstance(psiModule);
            var newStatement = CreateStatement(psiModule, settings, factory);

            // find caret marker in created statement
            var caretMarker = new TreeNodeMarker<IExpressionStatement>();
            var collector = new RecursiveElementCollector<IExpressionStatement>(
              expressionStatement => IsMarkerExpressionStatement(expressionStatement, CaretMarker));
            var caretNodes = collector.ProcessElement(newStatement).GetResults();
            if (caretNodes.Count == 1) caretMarker.Mark(caretNodes[0]);

            // replace marker statement with the new one
            newStatement = statement.ReplaceBy(newStatement);
            PutExpression(newStatement, expression);

            // find and remove caret marker node
            var caretNode = caretMarker.FindMarkedNode(newStatement);
            if (caretNode != null)
            {
              caretPosition = caretNode.GetDocumentRange().TextRange.StartOffset;
              LowLevelModificationUtil.DeleteChild(caretNode);
            }

            caretMarker.Dispose(newStatement);
            break;
          }
        });
      }

      AfterComplete(textControl, suffix, caretPosition);
    }

    [NotNull] protected abstract TStatement CreateStatement([NotNull] IPsiModule psiModule,
      [NotNull] IContextBoundSettingsStore settings, [NotNull] CSharpElementFactory factory);
    protected abstract void PutExpression(
      [NotNull] TStatement statement, [NotNull] ICSharpExpression expression);

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