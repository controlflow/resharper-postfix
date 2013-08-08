using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Hotspots;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.LiveTemplates;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.LiveTemplates;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.TemplateProviders
{
  [PostfixTemplateProvider("arg", "Parenthesizes current expression")]
  public class ArgumentExpressionTemplateProvider : IPostfixTemplateProvider
  {
    public void CreateItems(PostfixTemplateAcceptanceContext context, ICollection<ILookupItem> consumer)
    {
      if (context.ForceMode)
      {
        consumer.Add(new LookupItem(context.OuterExpression));
      }
    }

    private sealed class LookupItem : ExpressionPostfixLookupItem<ICSharpExpression>
    {
      public LookupItem([NotNull] PrefixExpressionContext context)
        : base("arg", context) { }

      protected override ICSharpExpression CreateExpression(
        CSharpElementFactory factory, ICSharpExpression expression)
      {
        return factory.CreateExpression("($0)", expression);
      }

      protected override void AfterComplete(
        ITextControl textControl, Suffix suffix, ICSharpExpression expression, int? caretPosition)
      {
        var parenthesizedExpression = (IParenthesizedExpression) expression;
        var hotspotInfo = new HotspotInfo(
          new TemplateField("invocation", 0),
          parenthesizedExpression.GetDocumentStartOffset().GetHotspotRange());

        var endSelectionRange = expression.GetDocumentRange().EndOffsetRange().TextRange;
        var session = LiveTemplatesManager.Instance.CreateHotspotSessionAtopExistingText(
          expression.GetSolution(), endSelectionRange, textControl,
          LiveTemplatesManager.EscapeAction.LeaveTextAndCaret, new[] { hotspotInfo });

        session.Execute();
      }
    }
  }
}