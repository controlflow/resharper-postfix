using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.LinqTools;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Feature.Services.Resources;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Pointers;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;
using JetBrains.UI.Icons;
using JetBrains.UI.RichText;
using JetBrains.Util;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems
{
  public abstract class PostfixLookupItem : ILookupItem
  {
    [NotNull] private readonly string myShortcut;
    [NotNull] private readonly ITreeNodePointer<ICSharpExpression> myExpression;
    [NotNull] private readonly ITreeNodePointer<IReferenceExpression> myReference;
    private readonly TextRange myReplaceRange;

    protected PostfixLookupItem([NotNull] string shortcut,
      [NotNull] PostfixTemplateAcceptanceContext context,
      [NotNull] PrefixExpressionContext expression)
    {
      myShortcut = shortcut;
      myExpression = expression.Expression.CreateTreeElementPointer();
      myReference = context.ReferenceExpression.CreateTreeElementPointer();
      myReplaceRange = context.ReplaceRange; // note: this is minimum replace range
    }

    public MatchingResult Match(string prefix, ITextControl textControl)
    {
      // todo: match "nn" with "notnull"
      return LookupUtil.MatchesPrefixSimple(myShortcut, prefix);
    }

    public void Accept(ITextControl textControl, TextRange nameRange,
      LookupItemInsertType insertType, Suffix suffix, ISolution solution, bool keepCaretStill)
    {
      var expression = myExpression.GetTreeNode();
      if (expression == null) return;

      // take required component while tree is valid
      var psiModule = expression.GetPsiModule();
      var settingsStore = expression.GetSettingsStore();

      // calculate textual range to remove
      var exprRange = myReplaceRange.JoinLeft(expression.GetDocumentRange().TextRange);
      var replaceRange = exprRange.Intersects(nameRange) ? exprRange.JoinRight(nameRange) : exprRange;

      // fix "x > 0.if" to "x > 0"
      var reference = myReference.GetTreeNode();
      if (reference == null) return;

      ICSharpExpression expressionCopy;
      if (expression.Contains(reference))
      {
        var marker = new TreeNodeMarker<IReferenceExpression>(reference);
        expressionCopy = expression.Copy(expression);
        var copy = marker.GetAndDispose(expressionCopy);
        var exprToFix = copy.QualifierExpression.NotNull();

        LowLevelModificationUtil.ReplaceChildRange(copy, copy, exprToFix);
        if (exprToFix.NextSibling is IErrorElement)
          LowLevelModificationUtil.DeleteChild(exprToFix.NextSibling);
      }
      else
      {
        expressionCopy = expression.Copy(expression);
      }

      ExpandPostfix(
        textControl, suffix, solution, replaceRange,
        psiModule, settingsStore, expressionCopy);
    }

    protected abstract void ExpandPostfix([NotNull] ITextControl textControl,
      [NotNull] Suffix suffix, [NotNull] ISolution solution, TextRange replaceRange,
      [NotNull] IPsiModule psiModule, [NotNull] IContextBoundSettingsStore settings,
      [NotNull] ICSharpExpression expression);

    protected virtual void AfterComplete(
      [NotNull] ITextControl textControl, [NotNull] Suffix suffix, int? caretPosition)
    {
      if (caretPosition != null)
        textControl.Caret.MoveTo(caretPosition.Value, CaretVisualPlacement.DontScrollIfVisible);

      suffix.Playback(textControl);
    }

    public IconId Image
    {
      get { return ServicesThemedIcons.LiveTemplate.Id; }
    }

    public RichText DisplayName { get { return myShortcut; } }
    public RichText DisplayTypeName { get { return null; } }
    public string OrderingString { get { return myShortcut; } }
    public string Identity { get { return myShortcut; } }
    public bool CanShrink { get { return false; } }
    public bool Shrink() { return false; }
    public void Unshrink() { }

    public TextRange GetVisualReplaceRange(ITextControl textControl, TextRange nameRange)
    {
      // note: prefix highlighter disallows highlighting to be any position
      return TextRange.InvalidRange;
    }

    public bool AcceptIfOnlyMatched(LookupItemAcceptanceContext itemAcceptanceContext)
    {
      return false;
    }

#if RESHARPER8
    public int Multiplier { get; set; }
    public bool IsDynamic { get { return false; } }
    public bool IgnoreSoftOnSpace { get; set; }
#endif
  }
}