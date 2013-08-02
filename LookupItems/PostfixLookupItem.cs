using System;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.LinqTools;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Feature.Services.Resources;
using JetBrains.ReSharper.Psi;
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
    [NotNull] private readonly Type myReferenceType;
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
      myReferenceType = context.Parent.PostfixReferenceNode.GetType();
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
      var expression = (ICSharpExpression) FindMarkedNode(
        solution, textControl, myExpressionRange.Range, nameRange, typeof(ICSharpExpression));
      if (expression == null)
      {
        // still can be parsed as IReferenceName
        var referenceName = FindMarkedNode(
          solution, textControl, myExpressionRange.Range, nameRange, typeof(IReferenceName));
        if (referenceName == null) return;

        expression = CSharpElementFactory
          .GetInstance(referenceName.GetPsiModule(), false)
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

      var reference = FindMarkedNode(
        solution, textControl, myReferenceRange.Range,
        nameRange, myReferenceType);

      // fix "x > 0.if" to "x > 0"
      ICSharpExpression exprCopy;
      if (reference != null && expression.Contains(reference))
      {
        // todo: check this in case a > 0.if  \r\n  Console.WriteLine
        var re = reference as IReferenceExpression;
        if (re != null)
        {
          var marker = new TreeNodeMarker<IReferenceExpression>(re);
          exprCopy = expression.Copy(expression);
          var copy = marker.GetAndDispose(exprCopy);
          var exprToFix = copy.QualifierExpression.NotNull();

          LowLevelModificationUtil.ReplaceChildRange(copy, copy, exprToFix);
          if (exprToFix.NextSibling is IErrorElement)
            LowLevelModificationUtil.DeleteChild(exprToFix.NextSibling);
        }
        else
        {
          var rn = reference as IReferenceName;
          if (rn != null)
          {
            var marker = new TreeNodeMarker<IReferenceName>(rn);
            exprCopy = expression.Copy(expression);
            var copy = marker.GetAndDispose(exprCopy);
            var exprToFix = copy.Qualifier.NotNull();

            LowLevelModificationUtil.ReplaceChildRange(copy, copy, exprToFix);
            if (exprToFix.NextSibling is IErrorElement)
              LowLevelModificationUtil.DeleteChild(exprToFix.NextSibling);
            if (exprToFix.Parent.NextSibling is IErrorElement)
              LowLevelModificationUtil.DeleteChild(exprToFix.Parent.NextSibling);
          }
          else
          {
            var xs = new RecursiveElementCollector<IErrorElement>();
            expression.ProcessDescendants(xs);
            var ys = xs.GetResults().Select(x => new TreeNodeMarker<IErrorElement>(x)).ToList();

            exprCopy = expression.Copy(expression);

            foreach (var treeNodeMarker in ys)
            {
              var t = treeNodeMarker.FindMarkedNode(exprCopy);
              if (t != null)
              {
                treeNodeMarker.Dispose(exprCopy);
                LowLevelModificationUtil.DeleteChild(t);
              }
            }
          }
        }
      }
      else
      {
        exprCopy = expression.IsPhysical()
          ? expression.Copy(expression)
          : expression;
      }

      ExpandPostfix(textControl, suffix, solution, replaceRange, psiModule, exprCopy);
    }

    [CanBeNull] private static ITreeNode FindMarkedNode(
      [NotNull] ISolution solution, [NotNull] ITextControl textControl,
      TextRange markerRange, TextRange nameRange, [NotNull] Type nodeType)
    {
      var node = TextControlToPsi.GetSourceTokenAtOffset(
        solution, textControl, markerRange.StartOffset);

      while (node != null)
      {
        if (nodeType.IsInstanceOfType(node))
        {
          var range = node.GetDocumentRange().TextRange;
          if (range == markerRange ||
              range == markerRange.SetEndTo(nameRange.EndOffset) ||
              range == markerRange.SetEndTo(nameRange.StartOffset))
          {
            return node;
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