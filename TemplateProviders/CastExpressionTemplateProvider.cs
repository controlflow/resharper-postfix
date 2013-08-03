using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Hotspots;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.LiveTemplates;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Macros;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Macros.Implementations;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.LiveTemplates;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.TemplateProviders
{
  [PostfixTemplateProvider("cast", "Casts current expression")]
  public class CastExpressionTemplateProvider : IPostfixTemplateProvider
  {
    public void CreateItems(PostfixTemplateAcceptanceContext context, ICollection<ILookupItem> consumer)
    {
      if (context.ForceMode)
      {
        consumer.Add(new LookupItem(context.OuterExpression));
      }
    }

    private sealed class LookupItem : ExpressionPostfixLookupItem<ICastExpression>
    {
      public LookupItem([NotNull] PrefixExpressionContext context)
        : base("cast", context) { }

      protected override ICastExpression CreateExpression(
        CSharpElementFactory factory, ICSharpExpression expression)
      {
        return (ICastExpression) factory.CreateExpression("(T) $0", expression);
      }

      protected override void AfterComplete(
        ITextControl textControl, Suffix suffix, ICastExpression expression, int? caretPosition)
      {
#if RESHARPER7
        var typeExpression = new MacroCallExpression(new SuggestVariableTypeMacro());
#else
        var typeExpression = new MacroCallExpressionNew(new GuessValueTypeMacroDef());
#endif

        var hotspotInfo = new HotspotInfo(
          new TemplateField("type", typeExpression, 0),
          expression.TargetType.GetDocumentRange().GetHotspotRange());

        var endSelectionRange = expression.GetDocumentRange().EndOffsetRange().TextRange;
        var session = LiveTemplatesManager.Instance.CreateHotspotSessionAtopExistingText(
          expression.GetSolution(), endSelectionRange, textControl,
          LiveTemplatesManager.EscapeAction.LeaveTextAndCaret, new[] {hotspotInfo});

        session.Execute();
      }
    }
  }
}