using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Hotspots;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.LiveTemplates;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Macros;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Macros.Implementations;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.LiveTemplates;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Xaml.Impl;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.Templates
{
  [PostfixTemplate(
    templateName: "forEach",
    description: "Iterates over enumerable collection",
    example: "foreach (var x in expr)")]
  public class ForEachLoopTemplate : IPostfixTemplate
  {
    public ILookupItem CreateItems(PostfixTemplateContext context)
    {
      var expressionContext = context.Expressions.LastOrDefault();
      if (expressionContext == null) return null;
      if (!expressionContext.CanBeStatement) return null;

      var typeIsEnumerable = context.ForceMode;
      if (!typeIsEnumerable)
      {
        if (!expressionContext.Type.IsResolved) return null;

        var predefined = expressionContext.Expression.GetPredefinedType();
        var rule = expressionContext.Expression.GetTypeConversionRule();
        if (rule.IsImplicitlyConvertibleTo(expressionContext.Type, predefined.IEnumerable))
          typeIsEnumerable = true;
      }

      if (!typeIsEnumerable)
      {
        var declaredType = expressionContext.Type as IDeclaredType;
        if (declaredType != null && !declaredType.IsUnknown)
        {
          var typeElement = declaredType.GetTypeElement();
          if (typeElement != null && typeElement.IsForeachEnumeratorPatternType())
            typeIsEnumerable = true;
        }
      }

      if (typeIsEnumerable)
      {
        return new ForEachItem(expressionContext);
      }

      return null;
    }

    private sealed class ForEachItem : KeywordStatementPostfixLookupItem<IForeachStatement>
    {
      public ForEachItem([NotNull] PrefixExpressionContext context) : base("forEach", context) { }

      protected override string Template
      {
        get { return "foreach(var x in expr)"; }
      }

      protected override void PlaceExpression(IForeachStatement statement,
        ICSharpExpression expression,
        CSharpElementFactory factory)
      {
        statement.Collection.ReplaceBy(expression);
      }

      protected override void AfterComplete(
        ITextControl textControl, Suffix suffix, IForeachStatement statement, int? caretPosition)
      {
        if (caretPosition == null) return;

        var iterator = statement.IteratorDeclaration;
        var typeExpression = new MacroCallExpressionNew(new SuggestVariableTypeMacroDef());
        var nameExpression = new MacroCallExpressionNew(new SuggestVariableNameMacroDef());

        var typeSpot = new HotspotInfo(
          new TemplateField("type", typeExpression, 0),
          iterator.VarKeyword.GetDocumentRange().GetHotspotRange());

        var nameSpot = new HotspotInfo(
          new TemplateField("name", nameExpression, 0),
          iterator.NameIdentifier.GetDocumentRange().GetHotspotRange());

        var session = LiveTemplatesManager.Instance.CreateHotspotSessionAtopExistingText(
          statement.GetSolution(), new TextRange(caretPosition.Value), textControl,
          LiveTemplatesManager.EscapeAction.LeaveTextAndCaret, new[] {typeSpot, nameSpot});

        // special case: handle [.] suffix
        if (suffix.HasPresentation && suffix.Presentation == '.')
        {
          session.AdviceFinished((_, terminationType) =>
          {
            if (terminationType == TerminationType.Finished)
            {
              var nameValue = session.Hotspots[1].CurrentValue;
              textControl.Document.InsertText(textControl.Caret.Offset(), nameValue);
              suffix.Playback(textControl);
            }
          });
        }

        session.Execute();
      }
    }
  }
}