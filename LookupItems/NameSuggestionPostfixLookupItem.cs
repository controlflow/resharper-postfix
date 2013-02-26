using System;
using System.Collections.Generic;
using JetBrains.Annotations;
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
  public sealed class NameSuggestionPostfixLookupItem : PostfixLookupItem
  {
    [NotNull] private readonly ISolution mySolution;
    [NotNull] private readonly IList<string> myNames;

    private const string NamePlaceholder = "$NAME$";

    public NameSuggestionPostfixLookupItem(
      [NotNull] string shortcut, [NotNull] string replaceTemplate, [NotNull] ICSharpExpression expression,
      PluralityKinds pluralityKinds = PluralityKinds.Single, ScopeKind scopeKind = ScopeKind.Common)
      : base(shortcut, replaceTemplate)
    {
      mySolution = expression.GetSolution();
      myNames = SuggestNamesFromExpression(expression, pluralityKinds, scopeKind);
    }

    protected override void AfterCompletion(
      ITextControl textControl, Suffix suffix, TextRange resultRange, string targetText, int caretOffset)
    {
      var placeholders = new List<TextRange>();
      for (var index = 0;; index++)
      {
        index = targetText.IndexOf(NamePlaceholder, index, StringComparison.Ordinal);
        if (index == -1) break;

        var range = new TextRange(resultRange.StartOffset + index);
        placeholders.Add(range.ExtendRight(NamePlaceholder.Length));
      }

      if (placeholders.Count == 0)
      {
        base.AfterCompletion(textControl, suffix, resultRange, targetText, caretOffset);
      }
      else
      {
        var nameField = new TemplateField("name", new NameSuggestionsExpression(myNames), 0);
        var hotspotInfo = new HotspotInfo(nameField, placeholders);
        var endRange = new TextRange(resultRange.StartOffset + caretOffset);

        var session = LiveTemplatesManager.Instance.CreateHotspotSessionAtopExistingText(
          mySolution, endRange, textControl, LiveTemplatesManager.EscapeAction.LeaveTextAndCaret, hotspotInfo);

        if (!suffix.IsEmpty)
          session.Finished += (_, terminationType) =>
          {
            if (terminationType == TerminationType.Finished)
              suffix.Playback(textControl);
          };

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
          SubrootPolicy = SubrootPolicy.Decompose,
          PluralityKind = kind
        });

      collection.Prepare(NamedElementKinds.Locals, scopeKind,
        new SuggestionOptions { DefaultName = "x", UniqueNameContext = expression });

      return collection.AllNames();
    }
  }
}