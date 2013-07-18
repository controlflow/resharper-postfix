using System;
using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Feature.Services.Resources;
using JetBrains.TextControl;
using JetBrains.UI.Icons;
using JetBrains.UI.RichText;
using JetBrains.Util;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems
{
  [Obsolete("To be removed")]
  public class PostfixLookupItemObsolete : ILookupItem
  {
    [NotNull] private readonly string myShortcut;
    [NotNull] private readonly string myReplaceTemplate;
    private TextRange myExpressionRange, myReplaceRange;

    public PostfixLookupItemObsolete(
      [NotNull] PostfixTemplateAcceptanceContext context,
      [NotNull] string shortcut, [NotNull] string replaceTemplate)
    {
      myShortcut = shortcut;
      myReplaceTemplate = replaceTemplate;
      myReplaceRange = context.MinimalReplaceRange;
      myExpressionRange = context.ExpressionRange;
    }

    public bool AcceptIfOnlyMatched(LookupItemAcceptanceContext itemAcceptanceContext)
    {
      return false;
    }

    public MatchingResult Match(string prefix, ITextControl textControl)
    {
      return LookupUtil.MatchesPrefixSimple(myShortcut, prefix);
    }

    public void Accept(
      ITextControl textControl, TextRange nameRange, LookupItemInsertType lookupItemInsertType,
      Suffix suffix, ISolution solution, bool keepCaretStill)
    {
      if (!myReplaceRange.IsValid || !myExpressionRange.IsValid) return;

      var replaceRange = myReplaceRange.Intersects(nameRange)
        ? new TextRange(myReplaceRange.StartOffset, nameRange.EndOffset)
        : myReplaceRange;

      var expressionText = textControl.Document.GetText(myExpressionRange);
      var targetText = myReplaceTemplate.Replace("$EXPR$", expressionText);

      var caretOffset = targetText.IndexOf("$CARET$", StringComparison.Ordinal);
      if (caretOffset == -1) caretOffset = targetText.Length;
      else targetText = targetText.Replace("$CARET$", string.Empty);

      textControl.Document.ReplaceText(replaceRange, targetText);

      var range = TextRange.FromLength(replaceRange.StartOffset, targetText.Length);
      AfterCompletion(textControl, solution, suffix, range, targetText, caretOffset);
    }

    protected virtual void AfterCompletion(
      [NotNull] ITextControl textControl, ISolution solution, [NotNull] Suffix suffix,
      TextRange resultRange, [NotNull] string targetText, int caretOffset)
    {
      textControl.Caret.MoveTo(
        resultRange.StartOffset + caretOffset, CaretVisualPlacement.DontScrollIfVisible);

      suffix.Playback(textControl);
    }

    public IconId Image { get { return ServicesThemedIcons.LiveTemplate.Id; } }

    public RichText DisplayName { get { return myShortcut; } }
    public RichText DisplayTypeName { get { return null; } }

    public TextRange GetVisualReplaceRange(ITextControl textControl, TextRange nameRange)
    {
      return TextRange.InvalidRange;
    }

    public bool CanShrink { get { return false; } }
    public bool Shrink() { return false; }
    public void Unshrink() { }

    public string OrderingString { get { return myShortcut; } }
#if RESHARPER8
    public int Multiplier { get; set; }
    public bool IsDynamic { get { return false; } }
    public bool IgnoreSoftOnSpace { get; set; }
#endif
    public string Identity { get { return myShortcut; } }
  }
}