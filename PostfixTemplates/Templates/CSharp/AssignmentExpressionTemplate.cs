using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Hotspots;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.LiveTemplates;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Templates;
using JetBrains.ReSharper.PostfixTemplates.CodeCompletion;
using JetBrains.ReSharper.PostfixTemplates.Contexts.CSharp;
using JetBrains.ReSharper.PostfixTemplates.LookupItems;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;

namespace JetBrains.ReSharper.PostfixTemplates.Templates.CSharp
{
  [PostfixTemplate(
    templateName: "to",
    description: "Assigns current expression to some variable",
    example: "lvalue = expr;")]
  public class AssignmentExpressionTemplate : IPostfixTemplate<CSharpPostfixTemplateContext>
  {
    [NotNull] private readonly LiveTemplatesManager myLiveTemplatesManager;

    public AssignmentExpressionTemplate([NotNull] LiveTemplatesManager liveTemplatesManager)
    {
      myLiveTemplatesManager = liveTemplatesManager;
    }

    public PostfixTemplateInfo TryCreateInfo(CSharpPostfixTemplateContext context)
    {
      if (context.IsPreciseMode) return null;

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

      return new PostfixTemplateInfo("to", outerExpression);
    }

    public PostfixTemplateBehavior CreateBehavior(PostfixTemplateInfo info)
    {
      return new CSharpPostfixAssignmentStatementBehavior(info, myLiveTemplatesManager);
    }

    private class CSharpPostfixAssignmentStatementBehavior : CSharpStatementPostfixTemplateBehavior<IExpressionStatement>
    {
      [NotNull] private readonly LiveTemplatesManager myLiveTemplatesManager;

      public CSharpPostfixAssignmentStatementBehavior([NotNull] PostfixTemplateInfo info, [NotNull] LiveTemplatesManager liveTemplatesManager)
        : base(info)
      {
        myLiveTemplatesManager = liveTemplatesManager;
      }

      protected override IExpressionStatement CreateStatement(CSharpElementFactory factory, ICSharpExpression expression)
      {
        return (IExpressionStatement) factory.CreateStatement("target = $0;", expression);
      }

      protected override void AfterComplete(ITextControl textControl, IExpressionStatement statement)
      {
        var expression = (IAssignmentExpression) statement.Expression;
        var templateField = new TemplateField("target", 0);
        var hotspotInfo = new HotspotInfo(templateField, expression.Dest.GetDocumentRange());

        var endRange = statement.GetDocumentRange().EndOffsetRange().TextRange;
        var session = myLiveTemplatesManager.CreateHotspotSessionAtopExistingText(
          statement.GetSolution(), endRange, textControl,
          LiveTemplatesManager.EscapeAction.RestoreToOriginalText, hotspotInfo);

        session.Execute();
      }
    }
  }
}