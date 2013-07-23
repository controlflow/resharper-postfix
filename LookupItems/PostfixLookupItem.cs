using JetBrains.Annotations;
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.LinqTools;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Feature.Services.Resources;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Services;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Text;
using JetBrains.TextControl;
using JetBrains.UI.Icons;
using JetBrains.UI.RichText;
using JetBrains.Util;
#if RESHARPER7
using JetBrains.ReSharper.Psi;
#else
using JetBrains.ReSharper.Psi.Modules;
#endif

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems
{
  public abstract class PostfixLookupItem : ILookupItem
  {
    [NotNull] private readonly string myShortcut;
    [NotNull] private readonly string myIdentifier;
    [NotNull] private readonly IRangeMarker myExpressionRange;
    [NotNull] private readonly IRangeMarker myReferenceRange;
    private readonly DocumentRange myReplaceRange;

    protected const string PostfixMarker = "POSTFIX_COMPLETION_MARKER";
    protected const string CaretMarker = "POSTFIX_COMPLETION_CARET";

    protected PostfixLookupItem(
      [NotNull] string shortcut, [NotNull] PrefixExpressionContext context)
    {
      myIdentifier = shortcut;
      myShortcut = shortcut.ToLowerInvariant();
      myExpressionRange = context.ExpressionRange.CreateRangeMarker();
      myReferenceRange = context.Parent.PostfixReferenceRange.CreateRangeMarker();
      myReplaceRange = context.ReplaceRange;
    }

    public MatchingResult Match(string prefix, ITextControl textControl)
    {
      return LookupUtil.MatchPrefix(new IdentifierMatcher(prefix), myIdentifier);
    }

    public void Accept(
      ITextControl textControl, TextRange nameRange, LookupItemInsertType insertType,
      Suffix suffix, ISolution solution, bool keepCaretStill)
    {
      var expression = FindMarkedNode<ICSharpExpression>(
        solution, textControl, myExpressionRange.Range, nameRange);
      if (expression == null)
      {
        // still can be parsed as IReferenceName
        var referenceName = FindMarkedNode<IReferenceName>(
          solution, textControl, myExpressionRange.Range, nameRange);
        if (referenceName == null) return;

        expression = CSharpElementFactory
          .GetInstance(referenceName, false)
          .CreateExpression(referenceName.GetText());
      }

      // take required component while tree is valid
      var psiModule = expression.GetPsiModule();

      // calculate textual range to remove
      var nameDocumentRange = new DocumentRange(textControl.Document, nameRange);

      // ReSharper disable once ImpureMethodCallOnReadonlyValueField
      var replaceRange = myReplaceRange.Intersects(nameDocumentRange)
        ? myReplaceRange.SetEndTo(nameDocumentRange.TextRange.EndOffset)
        : myReplaceRange;

      var reference = FindMarkedNode<IReferenceExpression>(
        solution, textControl, myReferenceRange.Range, nameRange);

      // fix "x > 0.if" to "x > 0"
      ICSharpExpression exprCopy;
      if (reference != null && expression.Contains(reference))
      {
        // todo: check this in case a > 0.if  \r\n  Console.WriteLine

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
        exprCopy = expression.IsPhysical()
          ? expression.Copy(expression)
          : expression;
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
          if (range == markerRange ||
              range == markerRange.SetEndTo(nameRange.EndOffset) ||
              range == markerRange.SetEndTo(nameRange.StartOffset))
          {
            return tNode;
          }
        }

        node = node.Parent;
      }

      return null;
    }

    protected abstract void ExpandPostfix([NotNull] ITextControl textControl,
      [NotNull] Suffix suffix, [NotNull] ISolution solution, DocumentRange replaceRange,
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