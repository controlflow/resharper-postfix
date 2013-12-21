using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.Settings;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems
{
  // todo: support razor
  // todo: support removal of ( )

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
      var newStatement = CreateStatement(factory, context.Expression);

      var targetStatement = context.GetContainingStatement();
      Assertion.AssertNotNull(targetStatement, "targetStatement != null");
      Assertion.Assert(targetStatement.IsPhysical(), "targetStatement.IsPhysical()");

      return targetStatement.ReplaceBy(newStatement);
    }

    [NotNull] protected abstract TStatement CreateStatement(
      [NotNull] CSharpElementFactory factory, [NotNull] ICSharpExpression expression);

    [ContractAnnotation("null => null"), CanBeNull]
    private ICSharpStatement UnwrapFromBraces(ITreeNode statement)
    {
      if (statement == null) return null;

      var blockStatement = statement as IBlock;
      if (blockStatement == null) return null;

      var statements = blockStatement.Statements;
      return (statements.Count == 1) ? statements[0] : null;
    }

    protected override void AfterComplete(ITextControl textControl, TStatement statement)
    {
      foreach (var child in statement.Children())
      {
        var usingStatement = myUseBraces
          ? (UnwrapFromBraces(child) as IUsingStatement)
          : (child as IUsingStatement);

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
      // todo: HOW ABOUT NO?
      {
        var endOffset = statement.GetDocumentRange().TextRange.EndOffset;
        textControl.Caret.MoveTo(endOffset, CaretVisualPlacement.DontScrollIfVisible);
      }
    }
  }
}