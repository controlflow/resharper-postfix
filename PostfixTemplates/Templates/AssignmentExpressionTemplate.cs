using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Hotspots;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.LiveTemplates;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.LiveTemplates;
using JetBrains.ReSharper.PostfixTemplates.LookupItems;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;

namespace JetBrains.ReSharper.PostfixTemplates.Templates
{
  [PostfixTemplate(
    templateName: "to",
    description: "Assigns current expression to some variable",
    example: "lvalue = expr;")]
  public class AssignmentExpressionTemplate : IPostfixTemplate
  {
    public IPostfixLookupItem CreateItem(PostfixTemplateContext context)
    {
      if (context.IsAutoCompletion) return null;

      var outerExpression = context.OuterExpression;
      if (outerExpression == null || !outerExpression.CanBeStatement) return null;

      for (ITreeNode node = outerExpression.Expression;;)
      {
        var assignmentExpression = node.GetContainingNode<IAssignmentExpression>();
        if (assignmentExpression == null) break;

        // disable 'here.to = "abc";'
        if (assignmentExpression.Dest.Contains(node)) return null;

        node = assignmentExpression;
      }

      return new AssignmentItem(outerExpression);
    }

    private class AssignmentItem : StatementPostfixLookupItem<IExpressionStatement>
    {
      [NotNull] private readonly LiveTemplatesManager myTemplatesManager;

      public AssignmentItem([NotNull] PrefixExpressionContext context) : base("to", context)
      {
        var executionContext = context.PostfixContext.ExecutionContext;
        myTemplatesManager = executionContext.LiveTemplatesManager;
      }

      protected override IExpressionStatement CreateStatement(CSharpElementFactory factory,
                                                              ICSharpExpression expression)
      {
        return (IExpressionStatement) factory.CreateStatement("target = $0;", expression);
      }

      protected override void AfterComplete(ITextControl textControl, IExpressionStatement statement)
      {
        var expression = (IAssignmentExpression) statement.Expression;
        var templateField = new TemplateField("target", 0);
        var hotspotInfo = new HotspotInfo(templateField, expression.Dest.GetDocumentRange());

        var endRange = statement.GetDocumentRange().EndOffsetRange().TextRange;
        var session = myTemplatesManager.CreateHotspotSessionAtopExistingText(
          statement.GetSolution(), endRange, textControl,
          LiveTemplatesManager.EscapeAction.RestoreToOriginalText, hotspotInfo);

        session.Execute();
      }
    }
  }
}