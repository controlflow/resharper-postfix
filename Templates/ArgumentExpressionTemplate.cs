using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Hotspots;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.LiveTemplates;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.LiveTemplates;
using JetBrains.ReSharper.PostfixTemplates.LookupItems;
using JetBrains.ReSharper.PostfixTemplates.Settings;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;
using JetBrains.Util;

// todo: disable inside .arg hotspot somehow...
// todo: disable here: 'foo.arg()'

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
      if (context.IsAutoCompletion) return null;

      // disable .arg template if .arg hotspot is enabled now
      var textControl = context.ExecutionContext.TextControl;
      if (textControl.GetData(PostfixArgTemplateExpansion) != null) return null;

      return new ArgumentItem(context.OuterExpression);
    }

    [NotNull] private static readonly Key<object> PostfixArgTemplateExpansion
      = new Key(typeof(ArgumentExpressionTemplate).FullName);

    private class ArgumentItem : ExpressionPostfixLookupItem<IInvocationExpression>
    {
      [NotNull] private readonly ILookupItemsOwner myLookupItemsOwner;
      [NotNull] private readonly LiveTemplatesManager myTemplatesManager;

      public ArgumentItem([NotNull] PrefixExpressionContext context) : base("arg", context)
      {
        var executionContext = context.PostfixContext.ExecutionContext;
        myLookupItemsOwner = executionContext.LookupItemsOwner;
        myTemplatesManager = executionContext.LiveTemplatesManager;
      }

      protected override IInvocationExpression CreateExpression(CSharpElementFactory factory,
                                                                ICSharpExpression expression)
      {
        return (IInvocationExpression) factory.CreateExpression("Method($0)", expression);
      }

      protected override void AfterComplete(ITextControl textControl, IInvocationExpression expression)
      {
        var invocationRange = expression.InvokedExpression.GetDocumentRange();
        var hotspotInfo = new HotspotInfo(new TemplateField("Method", 0), invocationRange);

        var argumentRange = expression.Arguments[0].Value.GetDocumentRange();
        var solution = expression.GetSolution();

        var marker = argumentRange.EndOffsetRange().CreateRangeMarker();
        var length = (marker.Range.EndOffset - invocationRange.TextRange.EndOffset);

        var session = myTemplatesManager.CreateHotspotSessionAtopExistingText(
          expression.GetSolution(), TextRange.InvalidRange, textControl,
          LiveTemplatesManager.EscapeAction.RestoreToOriginalText, hotspotInfo);

        var settings = expression.GetSettingsStore();
        var invokeParameterInfo = settings.GetValue(PostfixSettingsAccessor.InvokeParameterInfo);

        textControl.PutData(PostfixArgTemplateExpansion, string.Empty);

        session.Closed.Advise(Lifetime, _ =>
        {
          textControl.PutData(PostfixArgTemplateExpansion, null);

          var range = session.Hotspots[0].RangeMarker.Range;
          if (!range.IsValid) return;

          var endOffset = range.EndOffset + length;
          textControl.Caret.MoveTo(endOffset, CaretVisualPlacement.DontScrollIfVisible);

          if (invokeParameterInfo)
          {
            var parametersRange = TextRange.FromLength(range.EndOffset, length + 1);
            LookupUtil.ShowParameterInfo(
              solution, textControl, parametersRange, null, myLookupItemsOwner);
          }
        });

        session.Execute();
      }
    }
  }
}