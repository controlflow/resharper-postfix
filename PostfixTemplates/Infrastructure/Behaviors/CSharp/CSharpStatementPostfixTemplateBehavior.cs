using JetBrains.Annotations;
using JetBrains.Application.Progress;
using JetBrains.Application.Settings;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Options;
using JetBrains.ReSharper.Feature.Services.Util;
using JetBrains.ReSharper.PostfixTemplates.Contexts;
using JetBrains.ReSharper.PostfixTemplates.Contexts.CSharp;
using JetBrains.ReSharper.PostfixTemplates.Settings;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CodeStyle;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Pointers;
using JetBrains.ReSharper.Psi.Transactions;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.TextControl;
using JetBrains.Util;
using JetBrains.Util.Logging;

namespace JetBrains.ReSharper.PostfixTemplates.LookupItems
{
  // todo: check templates with braces completed by '{' suffix

  public abstract class CSharpStatementPostfixTemplateBehavior<TStatement> : PostfixTemplateBehavior
    where TStatement : class, ICSharpStatement
  {
    private readonly bool myUseBraces;

    protected CSharpStatementPostfixTemplateBehavior([NotNull] PostfixTemplateInfo info) : base(info)
    {
      myUseBraces = info.ExecutionContext.SettingsStore.GetValue(PostfixTemplatesSettingsAccessor.BracesForStatements);
    }

    private const string CaretTemplate = "return unchecked(checked(\"If you see this - please report a bug :(\"))";

    [NotNull] protected string EmbeddedStatementBracesTemplate
    {
      get { return myUseBraces ? "{" + CaretTemplate + "}" : CaretTemplate; }
    }

    protected string RequiredBracesTemplate
    {
      get { return "{" + CaretTemplate + "}"; }
    }

    protected override ITreeNode ExpandPostfix(PostfixExpressionContext context)
    {
      var csharpContext = (CSharpPostfixExpressionContext) context;
      var psiModule = csharpContext.PostfixContext.PsiModule;
      var psiServices = psiModule.GetPsiServices();
      var factory = CSharpElementFactory.GetInstance(psiModule);

      var targetStatement = csharpContext.GetContainingStatement();
      var expressionRange = csharpContext.Expression.GetDocumentRange();

      // Razor issue - hard to convert expression to statement
      if (!targetStatement.GetDocumentRange().IsValid())
      {
        var newStatement = psiServices.DoTransaction(ExpandCommandName, () =>
        {
          // todo: pass original context?
          var expression = csharpContext.Expression.GetOperandThroughParenthesis().NotNull();
          return CreateStatement(factory, expression);
        });

        var razorStatement = RazorUtil.FixExpressionToStatement(expressionRange, psiServices);
        if (razorStatement != null)
        {
          return psiServices.DoTransaction(ExpandCommandName, () =>
          {
            var statement = razorStatement.ReplaceBy(newStatement);

            // force Razor's bracing style
            var languageService = statement.Language.LanguageService().NotNull();
            var formatter = languageService.CodeFormatter.NotNull();
            formatter.Format(statement, CodeFormatProfile.SOFT, NullProgressIndicator.Instance);

            return statement;
          });
        }

        Logger.Fail("Failed to resolve target statement to replace");
        return null;
      }

      return psiServices.DoTransaction(ExpandCommandName, () =>
      {
        var expression = csharpContext.Expression.GetOperandThroughParenthesis().NotNull();
        var newStatement = CreateStatement(factory, expression);

        Assertion.AssertNotNull(targetStatement, "targetStatement != null");
        Assertion.Assert(targetStatement.IsPhysical(), "targetStatement.IsPhysical()");

        // Sometimes statements produced by templates are unfinished (for example, because of
        // parentheses insertion mode in R#), so the created statements has error element at and,
        // prefixed with single-characted whitespace. We remove this whitespace here:
        var errorElement = newStatement.LastChild as IErrorElement;
        if (errorElement != null)
        {
          var whitespaceNode = errorElement.PrevSibling as IWhitespaceNode;
          if (whitespaceNode != null && !whitespaceNode.IsNewLine && whitespaceNode.GetText() == " ")
          {
            using (WriteLockCookie.Create(newStatement.IsPhysical()))
            {
              LowLevelModificationUtil.DeleteChild(whitespaceNode);
            }
          }
        }

        return targetStatement.ReplaceBy(newStatement);
      });
    }

    [NotNull]
    protected abstract TStatement CreateStatement([NotNull] CSharpElementFactory factory, [NotNull] ICSharpExpression expression);

    [ContractAnnotation("null => null"), CanBeNull]
    private static ICSharpStatement UnwrapFromBraces(ITreeNode statement)
    {
      if (statement == null) return null;

      var blockStatement = statement as IBlock;
      if (blockStatement == null) return (statement as ICSharpStatement);

      var statements = blockStatement.Statements;
      return (statements.Count == 1) ? statements[0] : null;
    }

    protected sealed override void AfterComplete(ITextControl textControl, ITreeNode node)
    {
      AfterComplete(textControl, (TStatement) node);
    }

    protected virtual void AfterComplete(ITextControl textControl, TStatement statement)
    {
      PutStatementCaret(textControl, statement);
    }

    [CanBeNull]
    protected TStatement PutStatementCaret([NotNull] ITextControl textControl, [NotNull] TStatement statement)
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

          psiServices.Files.CommitAllDocuments();

          var fixedStatement = pointer.GetTreeNode();
          if (fixedStatement != null) return fixedStatement;

          var textRange = rangeMarker.DocumentRange.TextRange;
          if (textRange.IsValid)
          {
            foreach (var newStatement in TextControlToPsi.GetElements<TStatement>(
              psiServices.Solution, textControl.Document, textRange.StartOffset))
            {
              var offset = newStatement.GetDocumentStartOffset();
              if (offset.TextRange.StartOffset == textRange.StartOffset)
                return newStatement;
            }
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

    protected void FormatStatementOnSemicolon([NotNull] TStatement statement)
    {
      var settingsStore = statement.GetSettingsStore();
      if (settingsStore.GetValue(TypingAssistOptions.FormatStatementOnSemicolonExpression))
      {
        var psiServices = statement.GetPsiServices();
        using (PsiTransactionCookie.CreateAutoCommitCookieWithCachesUpdate(psiServices, "Format code"))
        {
          var languageService = statement.Language.LanguageService().NotNull();
          var codeFormatter = languageService.CodeFormatter.NotNull();

          codeFormatter.Format(statement, CodeFormatProfile.SOFT);
        }

        Assertion.Assert(statement.IsValid(), "statement.IsValid()");
        Assertion.Assert(statement.IsPhysical(), "statement.IsPhysical()");
      }
    }
  }
}