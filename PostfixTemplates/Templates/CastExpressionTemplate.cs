using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Hotspots;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.LiveTemplates;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Macros;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Macros.Implementations;
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
    templateName: "cast",
    description: "Surrounds expression with cast",
    example: "((SomeType) expr)")]
  public class CastExpressionTemplate : IPostfixTemplate
  {
    public ILookupItem CreateItem(PostfixTemplateContext context)
    {
      if (context.IsAutoCompletion) return null;

      var bestContext = CommonUtils.FindBestExpressionContext(context);
      if (bestContext == null) return null;

      return new CastItem(bestContext);
    }

    private sealed class CastItem : ExpressionPostfixLookupItem<IParenthesizedExpression>
    {
      [NotNull] private readonly LiveTemplatesManager myTemplatesManager;

      public CastItem([NotNull] PrefixExpressionContext context) : base("cast", context)
      {
        myTemplatesManager = context.PostfixContext.ExecutionContext.LiveTemplatesManager;
      }

      protected override IParenthesizedExpression CreateExpression(CSharpElementFactory factory,
                                                                   ICSharpExpression expression)
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