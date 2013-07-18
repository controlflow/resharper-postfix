using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Hotspots;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.LiveTemplates;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.LiveTemplates;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Naming.Extentions;
using JetBrains.ReSharper.Psi.Naming.Impl;
using JetBrains.ReSharper.Psi.Naming.Settings;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems
{
  public sealed class NameSuggestionPostfixLookupItem : PostfixLookupItemObsolete
  {
    [NotNull] private readonly ISolution mySolution;
    [NotNull] private readonly IList<string> myNames;

    private const string NamePlaceholder = "$NAME$";

    public NameSuggestionPostfixLookupItem(
      [NotNull] PostfixTemplateAcceptanceContext context, [NotNull] string shortcut,
      [NotNull] string replaceTemplate, [NotNull] ICSharpExpression expression,
      PluralityKinds pluralityKinds = PluralityKinds.Single, ScopeKind scopeKind = ScopeKind.Common)
      : base(context, shortcut, replaceTemplate)
    {
      mySolution = expression.GetSolution();
      myNames = SuggestNamesFromExpression(expression, pluralityKinds, scopeKind);
    }

    protected override void AfterCompletion(
      ITextControl textControl, ISolution solution, Suffix suffix,
      TextRange resultRange, string targetText, int caretOffset)
    {
#if RESHARPER8
      var placeholders = new List<DocumentRange>();
#else
      var placeholders = new List<TextRange>();
#endif

      for (var index = 0;; index++)
      {
        index = targetText.IndexOf(NamePlaceholder, index, StringComparison.Ordinal);
        if (index == -1) break;

#if RESHARPER8
        var range = new DocumentRange(textControl.Document, resultRange.StartOffset + index);
#else
        var range = new TextRange(resultRange.StartOffset + index);
#endif

        placeholders.Add(range.ExtendRight(NamePlaceholder.Length));
      }

      if (placeholders.Count == 0)
      {
        base.AfterCompletion(textControl, solution, suffix, resultRange, targetText, caretOffset);
      }
      else
      {
        var nameField = new TemplateField("name", new NameSuggestionsExpression(myNames), 0);
        var hotspotInfo = new HotspotInfo(nameField, placeholders);
        var endRange = new TextRange(resultRange.StartOffset + caretOffset);

        var session = LiveTemplatesManager.Instance.CreateHotspotSessionAtopExistingText(
          mySolution, endRange, textControl, LiveTemplatesManager.EscapeAction.LeaveTextAndCaret, hotspotInfo);

        if (!suffix.IsEmpty)
        {
          session.HotspotUpdated += delegate
          {
            if (session.IsFinished)
              suffix.Playback(textControl);
          };
        }

        session.Execute();
      }
    }

    [NotNull]
    public static IList<string> SuggestNamesFromExpression([NotNull] ICSharpExpression expression,
      PluralityKinds kind = PluralityKinds.Single, ScopeKind scopeKind = ScopeKind.Common)
    {
      var suggestionManager = expression.GetPsiServices().Naming.Suggestion;
      var sourceFile = expression.GetSourceFile();
      if (sourceFile == null) return new[] {"foo"};

      var collection = suggestionManager.CreateEmptyCollection(
        PluralityKinds.Single, CSharpLanguage.Instance, false, sourceFile);

      collection.Add(expression, new EntryOptions {
          SubrootPolicy = SubrootPolicy.Decompose, PluralityKind = kind });

      collection.Prepare(NamedElementKinds.Locals, scopeKind,
        new SuggestionOptions { DefaultName = "x", UniqueNameContext = expression });

      return collection.AllNames();
    }
  }
}