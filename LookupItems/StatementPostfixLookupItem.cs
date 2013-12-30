using JetBrains.Annotations;
using JetBrains.Application.Progress;
using JetBrains.Application.Settings;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.PostfixTemplates.Settings;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CodeStyle;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Pointers;
using JetBrains.ReSharper.Psi.Services;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;
using JetBrains.Util;
using JetBrains.Util.Logging;

namespace JetBrains.ReSharper.PostfixTemplates.LookupItems
{
  public abstract class StatementPostfixLookupItem<TStatement> : PostfixLookupItem<TStatement>
    where TStatement : class, ICSharpStatement
  {
    private readonly bool myUseBraces;

    protected StatementPostfixLookupItem([NotNull] string shortcut,
                                         [NotNull] PrefixExpressionContext context)
      : base(shortcut, context)
    {
      Assertion.Assert(context.CanBeStatement, "context.CanBeStatement");

      var settingsStore = context.PostfixContext.Reference.GetSettingsStore();
      myUseBraces = settingsStore.GetValue(PostfixSettingsAccessor.BracesForStatements);
    }

    private const string CaretTemplate =
      "return unchecked(checked(\"If you see this - please report a bug :(\"))";

    protected string EmbeddedStatementBracesTemplate
    {
      get { return myUseBraces ? "{" + CaretTemplate + "}" : CaretTemplate; }
    }

    protected string RequiredBracesTemplate
    {
      get { return "{" + CaretTemplate + "}"; }
    }

    protected override TStatement ExpandPostfix(PrefixExpressionContext context)
    {
      var psiModule = context.PostfixContext.PsiModule;
      var psiServices = psiModule.GetPsiServices();
      var factory = CSharpElementFactory.GetInstance(psiModule);

      var targetStatement = context.GetContainingStatement();
      var expressionRange = context.Expression.GetDocumentRange();

      // Razor issue - hard to convert expression to statement
      if (!targetStatement.GetDocumentRange().IsValid())
      {
        var newStatement = psiServices.DoTransaction(
          ExpandCommandName, () => CreateStatement(factory, context.Expression));

        var razorStatement = RazorUtil.FixExpressionToStatement(expressionRange, psiServices);
        if (razorStatement != null)
        {
          return psiServices.DoTransaction(ExpandCommandName, () =>
          {
            var statement = razorStatement.ReplaceBy(newStatement);

            // force Razor's bracing style
            var formatter = statement.Language.LanguageServiceNotNull().CodeFormatter.NotNull();
            formatter.Format(statement, CodeFormatProfile.SOFT, NullProgressIndicator.Instance);

            return statement;
          });
        }

        Logger.Fail("Failed to resolve target statement to replace");
        return null;
      }

      return psiServices.DoTransaction(ExpandCommandName, () =>
      {
        var newStatement = CreateStatement(factory, context.Expression);

        Assertion.AssertNotNull(targetStatement, "targetStatement != null");
        Assertion.Assert(targetStatement.IsPhysical(), "targetStatement.IsPhysical()");

        return targetStatement.ReplaceBy(newStatement);
      });
    }

    [NotNull]
    protected abstract TStatement CreateStatement([NotNull] CSharpElementFactory factory,
                                                  [NotNull] ICSharpExpression expression);

    [ContractAnnotation("null => null"), CanBeNull]
    private static ICSharpStatement UnwrapFromBraces(ITreeNode statement)
    {
      if (statement == null) return null;

      var blockStatement = statement as IBlock;
      if (blockStatement == null) return (statement as ICSharpStatement);

      var statements = blockStatement.Statements;
      return (statements.Count == 1) ? statements[0] : null;
    }

    protected override void AfterComplete(ITextControl textControl, TStatement statement)
    {
      PutStatementCaret(textControl, statement);
    }

    [CanBeNull]
    protected TStatement PutStatementCaret(
      [NotNull] ITextControl textControl, [NotNull] TStatement statement)
    {
      foreach (var child in statement.Children())
      {
        var returnStatement = UnwrapFromBraces(child) as IReturnStatement;
        if (returnStatement == null) continue;

        var uncheckedExpression = returnStatement.Value as IUncheckedExpression;
        if (uncheckedExpression == null) continue;

        var checkedExpression = uncheckedExpression.Operand as ICheckedExpression;
        if (checkedExpression == null) continue;

        if (checkedExpression.Operand is ILiteralExpression)
        {
          var pointer = statement.CreateTreeElementPointer();
          var rangeMarker = statement.GetDocumentRange().CreateRangeMarker();

          var psiServices = checkedExpression.GetPsiServices();
          var caretRange = returnStatement.GetDocumentRange().TextRange;

          // drop caret marker expression and commit PSI
          textControl.Caret.MoveTo(caretRange.StartOffset, CaretVisualPlacement.DontScrollIfVisible);
          textControl.Document.DeleteText(caretRange);

          psiServices.CommitAllDocuments();

          var fixedStatement = pointer.GetTreeNode();
          if (fixedStatement != null) return fixedStatement;

          var textRange = rangeMarker.DocumentRange.TextRange;
          if (textRange.IsValid)
          {
            return TextControlToPsi.GetElement<TStatement>(
              psiServices.Solution, textControl.Document, textRange.StartOffset);
          }

          return null;
        }
      }

      // at the end of the statement otherwise...
      {
        var endOffset = statement.GetDocumentRange().TextRange.EndOffset;
        textControl.Caret.MoveTo(endOffset, CaretVisualPlacement.DontScrollIfVisible);
        return statement;
      }
    }
  }
}