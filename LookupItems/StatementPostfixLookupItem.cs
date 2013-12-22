using System;
using JetBrains.Annotations;
using JetBrains.Application.Progress;
using JetBrains.Application.Settings;
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.Settings;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Razor.Impl.Tree;
using JetBrains.ReSharper.Psi.Razor.Tree;
using JetBrains.ReSharper.Psi.Services;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;
using JetBrains.Util;
using JetBrains.Util.Logging;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems
{
  // todo: support razor
  // todo: support removal of ( )?

  public abstract class StatementPostfixLookupItem<TStatement> : PostfixLookupItem<TStatement>
    where TStatement : class, ICSharpStatement
  {
    private readonly bool myUseBraces;

    protected StatementPostfixLookupItem(
      [NotNull] string shortcut, [NotNull] PrefixExpressionContext context)
      : base(shortcut, context)
    {
      var settingsStore = context.Parent.Reference.GetSettingsStore();
      myUseBraces = settingsStore.GetValue(PostfixSettingsAccessor.UseBracesForEmbeddedStatements);
    }

    protected string EmbeddedStatementBracesTemplate
    {
      get { return myUseBraces ? "{using(null){}}" : "using(null){}"; }
    }

    protected string RequiredBracesTemplate
    {
      get { return "{using(null){}}"; }
    }

    protected override TStatement ExpandPostfix(PrefixExpressionContext context)
    {
      var psiModule = context.Parent.ExecutionContext.PsiModule;
      var factory = CSharpElementFactory.GetInstance(psiModule);
      var psiServices = psiModule.GetPsiServices();

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
          return psiServices.DoTransaction(
            ExpandCommandName, () => razorStatement.ReplaceBy(newStatement));
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

    [NotNull] protected abstract TStatement CreateStatement(
      [NotNull] CSharpElementFactory factory, [NotNull] ICSharpExpression expression);

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
      foreach (var child in statement.Children())
      {
        var usingStatement = UnwrapFromBraces(child) as IUsingStatement;
        if (usingStatement == null) continue;

        var resourceExpression = usingStatement.Expressions.FirstOrDefault();
        if (resourceExpression is ILiteralExpression &&
            resourceExpression.ConstantValue.IsPureNull(usingStatement.Language))
        {
          var caretRange = usingStatement.GetDocumentRange().TextRange;
          textControl.Caret.MoveTo(caretRange.StartOffset, CaretVisualPlacement.DontScrollIfVisible);
          textControl.Document.DeleteText(caretRange);
          return;
        }
      }

      // at the end of the statement otherwise...
      {
        var endOffset = statement.GetDocumentRange().TextRange.EndOffset;
        textControl.Caret.MoveTo(endOffset, CaretVisualPlacement.DontScrollIfVisible);
      }
    }
  }
}