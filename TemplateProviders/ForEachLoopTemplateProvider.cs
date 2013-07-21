using System.Collections.Generic;
using System.Linq;
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
using JetBrains.ReSharper.Psi.Naming.Extentions;
using JetBrains.ReSharper.Psi.Naming.Impl;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Xaml.Impl;
using JetBrains.TextControl;
#if RESHARPER7
using JetBrains.ReSharper.Psi;
#else
using JetBrains.ReSharper.Psi.Modules;
#endif

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.TemplateProviders
{
  // todo: check for 'foreach pattern'
  // todo: support untyped collections
  // todo: infer type by indexer like F#

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
        {
          typeIsEnumerable = true;
        }
      }

      if (typeIsEnumerable)
      {
        consumer.Add(new LookupItem(exprContext));
      }
    }

    private sealed class LookupItem : KeywordStatementPostfixLookupItem<IForeachStatement>
    {
      public LookupItem([NotNull] PrefixExpressionContext context) : base("foreach", context) { }

      protected override string Keyword { get { return "foreach"; } }
      public override bool ShortcutIsCSharpStatementKeyword { get { return true; } }

      protected override IForeachStatement CreateStatement(IPsiModule psiModule, CSharpElementFactory factory)
      {
        var template = Keyword + (BracesInsertion
        ? "(var x in expr){" + CaretMarker + ";}"
        : "(var x in expr)" + CaretMarker + ";");

        return (IForeachStatement) factory.CreateStatement(template);
      }

      protected override void PlaceExpression(
        IForeachStatement statement, ICSharpExpression expression, CSharpElementFactory factory)
      {
        statement.Collection.ReplaceBy(expression);
      }

      protected override void AfterComplete(
        ITextControl textControl, Suffix suffix, IForeachStatement newStatement, int? caretPosition)
      {
        if (newStatement == null) return;

        var iterator = newStatement.IteratorDeclaration;
        var collection = newStatement.Collection;

        var suggestionManager = newStatement.GetPsiServices().Naming.Suggestion;
        var suggestion = suggestionManager.CreateEmptyCollection(
          PluralityKinds.Single, newStatement.Language, true, newStatement);

        suggestion.Add(collection, new EntryOptions {
          //PluralityKind = PluralityKinds.Plural,
          // SubrootPolicy = SubrootPolicy.Decompose
        });

        suggestion.Prepare(iterator.DeclaredElement,
          new SuggestionOptions { UniqueNameContext = newStatement, DefaultName = "x" });

        var caretFoo = new DocumentRange(textControl.Document, caretPosition.Value).CreateRangeMarker();

        // ????
        using (WriteLockCookie.Create())
          newStatement.GetPsiServices().DoTransaction("Boo", () =>
            iterator.SetName(suggestion.FirstName()));

        var memberNames = suggestion.AllNames();

        var suggestionsExpression = new NameSuggestionsExpression(memberNames);

        var hotspotInfo = new HotspotInfo(
          new TemplateField("memberName", suggestionsExpression, 0),
#if RESHARPER7
          iterator.NameIdentifier.GetDocumentRange().TextRange
#else
          iterator.NameIdentifier.GetDocumentRange()
#endif
);
        // todo: fix
        

        var endSelectionRange = newStatement.GetDocumentRange().EndOffsetRange().TextRange;
        var session = LiveTemplatesManager.Instance.CreateHotspotSessionAtopExistingText(
          newStatement.GetSolution(), endSelectionRange, textControl,
          LiveTemplatesManager.EscapeAction.LeaveTextAndCaret, new[] { hotspotInfo });

        session.Execute();
        session.Closed.Advise(session.Lifetime, args =>
          textControl.Caret.MoveTo(
            caretFoo.Range.StartOffset, CaretVisualPlacement.DontScrollIfVisible));

        
      }
    }
  }
}