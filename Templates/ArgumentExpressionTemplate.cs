using JetBrains.Annotations;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Hotspots;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.LiveTemplates;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.LiveTemplates;
using JetBrains.ReSharper.PostfixTemplates.LookupItems;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;
using JetBrains.Util;

// todo: disable inside .arg hotspot somehow...
// todo: disable here: 'foo.arg()'
// todo: disable over namespaces

namespace JetBrains.ReSharper.PostfixTemplates.Templates
{
  [PostfixTemplate(
    templateName: "arg",
    description: "Surrounds expression with invocation",
    example: "Method(expr)")]
  public class ArgumentExpressionTemplate : IPostfixTemplate
  {
    public ILookupItem CreateItem(PostfixTemplateContext context)
    {
      if (context.IsForceMode)
      {
        return new ArgumentItem(context.OuterExpression);
      }

      return null;
    }

    private class ArgumentItem : ExpressionPostfixLookupItem<ICSharpExpression>
    {
      [NotNull] private readonly ILookupItemsOwner myLookupItemsOwner;
      [NotNull] private readonly LiveTemplatesManager myTemplatesManager;

      public ArgumentItem([NotNull] PrefixExpressionContext context) : base("arg", context)
      {
        var executionContext = context.Parent.ExecutionContext;
        myLookupItemsOwner = executionContext.LookupItemsOwner;
        myTemplatesManager = executionContext.LiveTemplatesManager;
      }

      protected override ICSharpExpression CreateExpression(CSharpElementFactory factory,
                                                            ICSharpExpression expression)
      {
        return factory.CreateExpression("Method($0)", expression);
      }

      protected override void AfterComplete(ITextControl textControl, ICSharpExpression expression)
      {
        var invocationExpression = (IInvocationExpression) expression;
        var invocationRange = invocationExpression.InvokedExpression.GetDocumentRange();
        var hotspotInfo = new HotspotInfo(new TemplateField("Method", 0), invocationRange.GetHotspotRange());

        var argument = invocationExpression.Arguments[0];
        var argumentRange = argument.Value.GetDocumentRange();

        var solution = expression.GetSolution();
        var marker = argumentRange.EndOffsetRange().CreateRangeMarker();
        var length = (marker.Range.EndOffset - invocationRange.TextRange.EndOffset);

        var session = myTemplatesManager.CreateHotspotSessionAtopExistingText(
          expression.GetSolution(), TextRange.InvalidRange, textControl,
          LiveTemplatesManager.EscapeAction.RestoreToOriginalText, new[] {hotspotInfo});

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