using JetBrains.Annotations;
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.LinqTools;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Feature.Services.Resources;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Services;
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
    [NotNull] private readonly IRangeMarker myExpressionRange;
    [NotNull] private readonly IRangeMarker myReferenceRange;
    private readonly TextRange myReplaceRange;

    protected const string PostfixMarker = "POSTFIX_COMPLETION_MARKER";
    protected const string CaretMarker = "POSTFIX_COMPLETION_CARET";

    protected PostfixLookupItem(
      [NotNull] string shortcut, [NotNull] PrefixExpressionContext context)
    {
      myShortcut = shortcut;
      myExpressionRange = context.Expression.GetDocumentRange().CreateRangeMarker();
      myReferenceRange = context.Reference.GetDocumentRange().CreateRangeMarker();
      myReplaceRange = context.ReplaceRange;
    }

    public MatchingResult Match(string prefix, ITextControl textControl)
    {
      // todo: match "nn" with "notnull"
      return LookupUtil.MatchesPrefixSimple(myShortcut, prefix);
    }

    public virtual bool ShortcutIsCSharpStatementKeyword
    {
      get { return false; }
    }

    public void Accept(ITextControl textControl, TextRange nameRange,
      LookupItemInsertType insertType, Suffix suffix, ISolution solution, bool keepCaretStill)
    {
      var expression = FindMarkedNode<ICSharpExpression>(
        solution, textControl, myExpressionRange.Range, nameRange);
      if (expression == null) return;

      // take required component while tree is valid
      var psiModule = expression.GetPsiModule();

      // calculate textual range to remove
      var replaceRange = myReplaceRange.Intersects(nameRange)
        ? myReplaceRange.JoinRight(nameRange)
        : myReplaceRange;

      var reference = FindMarkedNode<IReferenceExpression>(
        solution, textControl, myReferenceRange.Range, nameRange);

      // fix "x > 0.if" to "x > 0"
      ICSharpExpression exprCopy;
      if (reference != null && expression.Contains(reference))
      {
        var marker = new TreeNodeMarker<IReferenceExpression>(reference);
        exprCopy = expression.Copy(expression);
        var copy = marker.GetAndDispose(exprCopy);
        var exprToFix = copy.QualifierExpression.NotNull();

        LowLevelModificationUtil.ReplaceChildRange(copy, copy, exprToFix);
        if (exprToFix.NextSibling is IErrorElement)
          LowLevelModificationUtil.DeleteChild(exprToFix.NextSibling);
      }
      else
      {
        exprCopy = expression.Copy(expression);
      }

      ExpandPostfix(textControl, suffix, solution, replaceRange, psiModule, exprCopy);
    }

    [CanBeNull] private TTreeNode FindMarkedNode<TTreeNode>([NotNull] ISolution solution,
      [NotNull] ITextControl textControl, TextRange markerRange, TextRange nameRange)
      where TTreeNode : class, ITreeNode
    {
      var node = TextControlToPsi.GetSourceTokenAtOffset(
        solution, textControl, markerRange.StartOffset);

      while (node != null)
      {
        var tNode = node as TTreeNode;
        if (tNode != null)
        {
          var range = tNode.GetDocumentRange().TextRange;
          if (range == markerRange || (ShortcutIsCSharpStatementKeyword
            ? range == markerRange.SetEndTo(nameRange.StartOffset)
            : range == markerRange.JoinRight(nameRange)))
          {
            return tNode;
          }
        }

        node = node.Parent;
      }

      return null;
    }

    protected abstract void ExpandPostfix([NotNull] ITextControl textControl,
      [NotNull] Suffix suffix, [NotNull] ISolution solution, TextRange replaceRange,
      [NotNull] IPsiModule psiModule, [NotNull] ICSharpExpression expression);

    protected virtual void AfterComplete(
      [NotNull] ITextControl textControl, [NotNull] Suffix suffix, int? caretPosition)
    {
      if (caretPosition != null)
      {
        textControl.Caret.MoveTo(
          caretPosition.Value, CaretVisualPlacement.DontScrollIfVisible);
      }

      ReplaySuffix(textControl, suffix);
    }

    protected virtual void ReplaySuffix(
      [NotNull] ITextControl textControl, [NotNull] Suffix suffix)
    {
      suffix.Playback(textControl);
    }

    public IconId Image { get { return ServicesThemedIcons.LiveTemplate.Id; } }
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