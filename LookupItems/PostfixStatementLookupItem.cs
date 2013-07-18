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
  public class PostfixStatementLookupItem : PostfixLookupItem
  {
    public PostfixStatementLookupItem([NotNull] string shortcut,
      [NotNull] PostfixTemplateAcceptanceContext context,
      [NotNull] PrefixExpressionContext expression)
      : base(shortcut, context, expression) { }

    protected override void ExpandPostfix(
      ITextControl textControl, Suffix suffix, ISolution solution, TextRange replaceRange,
      IPsiModule psiModule, IContextBoundSettingsStore settings, ICSharpExpression expression)
    {
      textControl.Document.ReplaceText(replaceRange, "POSTFIX;");
      solution.GetPsiServices().CommitAllDocuments();

      int? caretPos = null;

      using (WriteLockCookie.Create())
        solution.GetPsiServices().Transactions.Execute("AAAA", () =>
        {
          var re = TextControlToPsi.GetElements<IExpressionStatement>(
            solution, textControl.Document, replaceRange.StartOffset);

          foreach (var statement in re)
          {
            if (IsMarkerExpressionStatement(statement, "POSTFIX"))
            {
              var factory = CSharpElementFactory.GetInstance(psiModule);
              var ifStatement = (IIfStatement)factory.CreateStatement("if (expr){CARET;}");

              var c = new RecursiveElementCollector<IExpressionStatement>(es => IsMarkerExpressionStatement(es, "CARET"));
              var cm = new TreeNodeMarker<IExpressionStatement>();

              var caretNode = c.ProcessElement(ifStatement).GetResults();
              if (caretNode.Count == 1)
              {
                cm.Mark(caretNode[0]);
              }

              ifStatement = statement.ReplaceBy(ifStatement);
              ifStatement.Condition.ReplaceBy(expression);

              var cnode = cm.FindMarkedNode(ifStatement);
              if (cnode != null)
              {
                var pos = cnode.GetDocumentRange().TextRange.StartOffset;
                LowLevelModificationUtil.DeleteChild(cnode);
                caretPos = pos;
              }

              cm.Dispose(ifStatement);
            }
          }
        });

      AfterComplete(textControl, suffix, caretPos);
    }

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