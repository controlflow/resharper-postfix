using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Hotspots;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.LiveTemplates;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Macros;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.LiveTemplates;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Xaml.Impl;
using JetBrains.TextControl;
using JetBrains.Util;
#if RESHARPER7
using JetBrains.ReSharper.Psi;
#else
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Macros.Implementations;
#endif

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.TemplateProviders
{
  [PostfixTemplateProvider("foreach", "Iterating over expressions of collection type")]
  public class ForEachLoopTemplateProvider : IPostfixTemplateProvider
  {
    public void CreateItems(PostfixTemplateAcceptanceContext context, ICollection<ILookupItem> consumer)
    {
      var exprContext = context.PossibleExpressions.LastOrDefault();
      if (exprContext == null || !exprContext.CanBeStatement) return;

      var typeIsEnumerable = context.ForceMode;
      if (!typeIsEnumerable)
      {
        var predefined = exprContext.Expression.GetPredefinedType();
        var rule = exprContext.Expression.GetTypeConversionRule();
        if (rule.IsImplicitlyConvertibleTo(exprContext.Type, predefined.IEnumerable))
          typeIsEnumerable = true;
      }

      if (!typeIsEnumerable)
      {
        var declaredType = exprContext.Type as IDeclaredType;
        if (declaredType != null && !declaredType.IsUnknown)
        {
          var typeElement = declaredType.GetTypeElement();
          if (typeElement != null && typeElement.IsForeachEnumeratorPatternType())
          {
            typeIsEnumerable = true;
          }
        }
      }

      if (typeIsEnumerable)
      {
        consumer.Add(new LookupItem(exprContext));
      }
    }

    // todo: special behavior for [.] suffix?
    private sealed class LookupItem : KeywordStatementPostfixLookupItem<IForeachStatement>
    {
      public LookupItem([NotNull] PrefixExpressionContext context) : base("foreach", context) { }

      protected override string Template { get { return "foreach(var x in expr)"; } }
      public override bool ShortcutIsCSharpStatementKeyword { get { return true; } }

      protected override void PlaceExpression(
        IForeachStatement statement, ICSharpExpression expression, CSharpElementFactory factory)
      {
        statement.Collection.ReplaceBy(expression);
      }

      protected override void AfterComplete(
        ITextControl textControl, Suffix suffix, IForeachStatement newStatement, int? caretPosition)
      {
        if (newStatement == null || caretPosition == null) return;

        var iterator = newStatement.IteratorDeclaration;

#if RESHARPER7
        var typeExpression = new MacroCallExpression(new SuggestVariableTypeMacro());
        var nameExpression = new MacroCallExpression(new SuggestVariableNameMacro());
#else
        var typeExpression = new MacroCallExpressionNew(new SuggestVariableTypeMacroDef());
        var nameExpression = new MacroCallExpressionNew(new SuggestVariableNameMacroDef());
#endif

        var typeSpot = new HotspotInfo(
          new TemplateField("type", typeExpression, 0),
          iterator.VarKeyword.GetDocumentRange().GetHotspotRange());

        var nameSpot = new HotspotInfo(
          new TemplateField("name", nameExpression, 0),
          iterator.NameIdentifier.GetDocumentRange().GetHotspotRange());

        var session = LiveTemplatesManager.Instance.CreateHotspotSessionAtopExistingText(
          newStatement.GetSolution(), new TextRange(caretPosition.Value), textControl,
          LiveTemplatesManager.EscapeAction.LeaveTextAndCaret, new[] { typeSpot, nameSpot });

        session.Execute();
      }
    }
  }
}