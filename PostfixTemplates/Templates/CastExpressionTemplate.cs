using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Hotspots;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.LiveTemplates;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Macros;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Macros.Implementations;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Templates;
using JetBrains.ReSharper.PostfixTemplates.LookupItems;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;

namespace JetBrains.ReSharper.PostfixTemplates.Templates
{
  [PostfixTemplate(
    templateName: "cast",
    description: "Surrounds expression with cast",
    example: "((SomeType) expr)")]
  public class CastExpressionTemplate : IPostfixTemplate
  {
    public ILookupItem CreateItem(PostfixTemplateContext context)
    {
      if (context.IsAutoCompletion) return null;

      var expressions = CommonUtils.FindExpressionWithValuesContexts(context);
      if (expressions.Length == 0) return null;

      return new CastItem(expressions, context);
    }

    private sealed class CastItem : ExpressionPostfixLookupItem<IParenthesizedExpression>
    {
      [NotNull] private readonly LiveTemplatesManager myTemplatesManager;

      public CastItem([NotNull] PrefixExpressionContext[] contexts, [NotNull] PostfixTemplateContext postfixContext)
        : base("cast", contexts)
      {
        myTemplatesManager = postfixContext.ExecutionContext.LiveTemplatesManager;
      }

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
        var session = myTemplatesManager.CreateHotspotSessionAtopExistingText(
          expression.GetSolution(), endRange, textControl,
          LiveTemplatesManager.EscapeAction.LeaveTextAndCaret, hotspotInfo);

        session.Execute();
      }
    }
  }
}