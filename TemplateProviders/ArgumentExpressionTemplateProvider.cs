using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Hotspots;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.LiveTemplates;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.LiveTemplates;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;
using JetBrains.Util;

// todo: disable inside .arg hotspot somehow...
// todo: disable here: 'foo.arg()'

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
      var expressionContext = context.OuterExpression;

      if (context.ForceMode)
      {
        consumer.Add(new LookupItem(expressionContext, context.LookupItemsOwner));
      }
      else if (expressionContext.CanBeStatement)
      {
        // filter out expressions, unlikely suitable as arguments
        if (!CommonUtils.IsNiceExpression(expressionContext.Expression)) return;

        // foo.Bar().Baz.arg
        consumer.Add(new LookupItem(expressionContext, context.LookupItemsOwner));
      }
    }

    private sealed class LookupItem : ExpressionPostfixLookupItem<ICSharpExpression>
    {
      [NotNull] private readonly ILookupItemsOwner myLookupItemsOwner;

      public LookupItem(
        [NotNull] PrefixExpressionContext context,
        [NotNull] ILookupItemsOwner lookupItemsOwner) : base("arg", context)
      {
        myLookupItemsOwner = lookupItemsOwner;
      }

      protected override ICSharpExpression CreateExpression(
        CSharpElementFactory factory, ICSharpExpression expression)
      {
        return factory.CreateExpression("Method($0)", expression);
      }

      protected override void AfterComplete(
        ITextControl textControl, Suffix suffix, ICSharpExpression expression, int? caretPosition)
      {
        var invocationExpression = (IInvocationExpression) expression;
        var invocationRange = invocationExpression.InvokedExpression.GetDocumentRange();
        var hotspotInfo = new HotspotInfo(
          new TemplateField("Method", 0), invocationRange.GetHotspotRange());

        var argument = invocationExpression.Arguments[0];
        var argumentRange = argument.Value.GetDocumentRange();

        var solution = expression.GetSolution();
        var marker = argumentRange.EndOffsetRange().CreateRangeMarker();
        var length = (marker.Range.EndOffset - invocationRange.TextRange.EndOffset);

        var session = LiveTemplatesManager.Instance.CreateHotspotSessionAtopExistingText(
          expression.GetSolution(), TextRange.InvalidRange, textControl,
          LiveTemplatesManager.EscapeAction.RestoreToOriginalText, new[] { hotspotInfo });

        session.AdviceFinished((sess, type) =>
        {
          var invocation = sess.Hotspots[0].RangeMarker.Range;
          if (!invocation.IsValid) return;

          textControl.Caret.MoveTo(
            invocation.EndOffset + length, CaretVisualPlacement.DontScrollIfVisible);

          var range = TextRange.FromLength(invocation.EndOffset, length + 1);
          LookupUtil.ShowParameterInfo(
            solution, textControl, range, null, myLookupItemsOwner);
        });

        session.Execute();
      }
    }
  }
}
