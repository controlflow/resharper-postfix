using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Hotspots;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.LiveTemplates;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Macros;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Macros.Implementations;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Templates;
using JetBrains.ReSharper.PostfixTemplates.Contexts.CSharp;
using JetBrains.ReSharper.PostfixTemplates.LookupItems;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;

namespace JetBrains.ReSharper.PostfixTemplates.Templates.CSharp
{
  [PostfixTemplate(
    templateName: "cast",
    description: "Surrounds expression with cast",
    example: "((SomeType) expr)")]
  public class CastExpressionTemplate : IPostfixTemplate<CSharpPostfixTemplateContext>
  {
    public PostfixTemplateInfo TryCreateInfo(CSharpPostfixTemplateContext context)
    {
      if (context.IsPreciseMode) return null;

      var expressions = CSharpPostfixUtis.FindExpressionWithValuesContexts(context);
      if (expressions.Length == 0) return null;

      return new PostfixTemplateInfo("cast", expressions);
    }

    public PostfixTemplateBehavior CreateBehavior(PostfixTemplateInfo info)
    {
      return new CSharpPostfixCastExpressionBehavior(info);
    }

    private sealed class CSharpPostfixCastExpressionBehavior : CSharpExpressionPostfixTemplateBehavior<IParenthesizedExpression>
    {
      public CSharpPostfixCastExpressionBehavior([NotNull] PostfixTemplateInfo info) : base(info) { }

      protected override string ExpressionSelectTitle
      {
        get { return "Select expression to cast"; }
      }

      protected override IParenthesizedExpression CreateExpression(CSharpElementFactory factory, ICSharpExpression expression)
      {
        return (IParenthesizedExpression) factory.CreateExpression("((T) $0)", expression);
      }

      protected override void AfterComplete(ITextControl textControl, IParenthesizedExpression expression)
      {
        var castExpression = (ICastExpression) expression.Expression;

        var expectedTypeMacro = new MacroCallExpressionNew(new GuessExpectedTypeMacroDef());
        var hotspotInfo = new HotspotInfo(
          new TemplateField("T", expectedTypeMacro, 0),
          castExpression.TargetType.GetDocumentRange());

        var endRange = expression.GetDocumentRange().EndOffsetRange().TextRange;
        var session = Info.ExecutionContext.LiveTemplatesManager.CreateHotspotSessionAtopExistingText(
          expression.GetSolution(), endRange, textControl,
          LiveTemplatesManager.EscapeAction.LeaveTextAndCaret, hotspotInfo);

        session.Execute();
      }
    }
  }
}