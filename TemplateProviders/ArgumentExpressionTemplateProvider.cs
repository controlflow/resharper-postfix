using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application;
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
using JetBrains.Threading;
using JetBrains.Util;
using EternalLifetime = JetBrains.DataFlow.EternalLifetime;

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
        var hotspotInfo = new HotspotInfo(new TemplateField("Method", 0),
          parenthesizedExpression.GetDocumentStartOffset().GetHotspotRange());

        var marker = parenthesizedExpression.Expression.GetDocumentRange().EndOffsetRange().CreateRangeMarker();

        var len = marker.Range.EndOffset - parenthesizedExpression.GetDocumentStartOffset().TextRange.StartOffset;

        
        var session = LiveTemplatesManager.Instance.CreateHotspotSessionAtopExistingText(
          expression.GetSolution(), TextRange.InvalidRange, textControl,
          LiveTemplatesManager.EscapeAction.RestoreToOriginalText, new[] { hotspotInfo });

        session.Closed.Advise(EternalLifetime.Instance, args =>
        {
          //Shell.Instance.Locks.QueueAt(
          //  EternalLifetime.Instance, "aaa", TimeSpan.FromMilliseconds(100), () =>
          {
            var a = session.Hotspots[0].DriverRangeMarker.Range;
            if (a.IsValid)
            {

              textControl.Caret.MoveTo(
                a.EndOffset + len, CaretVisualPlacement.DontScrollIfVisible);
            }
          }
          //);
        });

        session.Execute();
      }
    }
  }
}