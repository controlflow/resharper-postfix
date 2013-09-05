using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Hotspots;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.LiveTemplates;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.LiveTemplates;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.TemplateProviders
{
  [PostfixTemplateProvider(
    templateName: "arg",
    description: "Surrounds expression with invocation",
    example: "Method(expr)")]
  public class ArgumentExpressionTemplateProvider : IPostfixTemplateProvider
  {
    public void CreateItems(
      PostfixTemplateAcceptanceContext context, ICollection<ILookupItem> consumer)
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
        return factory.CreateExpression("Method($0)", expression);
      }

      protected override void AfterComplete(
        ITextControl textControl, Suffix suffix, ICSharpExpression expression, int? caretPosition)
      {
        var invocationExpression = (IInvocationExpression) expression;
        var invocationRange = invocationExpression.GetDocumentStartOffset();
        var hotspotInfo = new HotspotInfo(
          new TemplateField("Method", 0), invocationRange.GetHotspotRange());

        var expressionRange = invocationExpression.InvokedExpression.GetDocumentRange();

        var marker = expressionRange.EndOffsetRange().CreateRangeMarker();
        var length = (marker.Range.EndOffset - invocationRange.TextRange.StartOffset);

        var session = LiveTemplatesManager.Instance.CreateHotspotSessionAtopExistingText(
          expression.GetSolution(), TextRange.InvalidRange, textControl,
          LiveTemplatesManager.EscapeAction.RestoreToOriginalText, new[] { hotspotInfo });

        session.AdviceFinished((sess, type) =>
        {
          var invocation = sess.Hotspots[0].RangeMarker.Range;
          if (!invocation.IsValid) return;

          textControl.Caret.MoveTo(
            invocation.EndOffset + length, CaretVisualPlacement.DontScrollIfVisible);
        });

        session.Execute();
      }
    }
  }
}