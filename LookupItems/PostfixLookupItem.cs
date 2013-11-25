using System;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.TemplateProviders;
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
using JetBrains.ReSharper.Psi.Modules;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems
{
  public abstract class PostfixLookupItem : PostfixLookupItemBase, ILookupItem
  {
    [NotNull] private readonly string myShortcut;
    [NotNull] private readonly string myIdentifier;
    [NotNull] private readonly IRangeMarker myExpressionRange;
    [NotNull] private readonly IRangeMarker myReferenceRange;
    [NotNull] private readonly Type myReferenceType;
    private readonly DocumentRange myReplaceRange;
    private readonly bool myWasReparsed;

    protected const string PostfixMarker = "POSTFIX_COMPLETION_MARKER";
    protected const string CaretMarker = "POSTFIX_COMPLETION_CARET";

    protected PostfixLookupItem(
      [NotNull] string shortcut, [NotNull] PrefixExpressionContext context)
    {
      myIdentifier = shortcut;
      myShortcut = shortcut.ToLowerInvariant();
      myWasReparsed = !context.Expression.IsPhysical();

      var shift = myWasReparsed ? +2 : 0;
      myExpressionRange = context.ExpressionRange.CreateRangeMarker();
      myReferenceRange = context.Parent.PostfixReferenceRange.CreateRangeMarker();
      myReferenceType = context.Parent.PostfixReferenceNode.GetType();
      myReplaceRange = context.ReplaceRange.ExtendRight(shift);
    }

    public MatchingResult Match(string prefix, ITextControl textControl)
    {
      return LookupUtil.MatchPrefix(new IdentifierMatcher(prefix), myIdentifier);
    }

    protected virtual bool RemoveSemicolon { get { return false; } }

    public void Accept(
      ITextControl textControl, TextRange nameRange,
      LookupItemInsertType insertType, Suffix suffix,
      ISolution solution, bool keepCaretStill)
    {
      // find target expression after code completion
      var expressionRange = myExpressionRange.Range;

      if (myWasReparsed)
      {
        textControl.Document.ReplaceText(nameRange, "__");
        solution.GetPsiServices().CommitAllDocuments();
        nameRange = TextRange.FromLength(nameRange.StartOffset, 2);
      }

      var expression = (ICSharpExpression) FindMarkedNode(
        solution, textControl, expressionRange, nameRange, typeof(ICSharpExpression));

      if (expression == null)
      {
        // still can be parsed as IReferenceName
        var referenceName = (IReferenceName) FindMarkedNode(
          solution, textControl, expressionRange, nameRange, typeof(IReferenceName));

        if (referenceName == null) return;

        // reparse IReferenceName as ICSharpExpression
        var factory = CSharpElementFactory.GetInstance(referenceName.GetPsiModule(), false);
        expression = factory.CreateExpression(referenceName.GetText());
      }

      // take required component while tree is valid
      var psiModule = expression.GetPsiModule();

      var reference = FindMarkedNode(
        solution, textControl, myReferenceRange.Range, nameRange, myReferenceType);

      // Razor.{caret} case
      if (reference == expression && myWasReparsed)
      {
        var parentReference = reference.Parent as IReferenceExpression;
        if (parentReference != null && parentReference.NameIdentifier.Name == "__")
          reference = parentReference;
      }

      // calculate textual range to remove
      var replaceRange = CalculateRangeToRemove(nameRange, expression, reference);

      // fix "x > 0.if" to "x > 0"
      ICSharpExpression expressionCopy;
      if (reference != null && expression.Contains(reference))
      {
        expressionCopy = FixExpression(expression, reference);
      }
      else
      {
        expressionCopy = expression.IsPhysical() ? expression.Copy(expression) : expression;
      }

      Assertion.Assert(!expressionCopy.IsPhysical(), "expressionCopy is physical");

      ExpandPostfix(
        textControl, suffix, solution,
        replaceRange, psiModule, expressionCopy);
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
          if (range == markerRange
              || range == markerRange.SetEndTo(nameRange.EndOffset)
              || range == markerRange.SetEndTo(nameRange.StartOffset))
          {
            return node;
          }

          // "x as T.   \r\nvar t = ..." case
          if (node is IIsExpression || node is IAsExpression)
          {
            var typeUsage = node.LastChild as IUserTypeUsage;
            if (typeUsage != null)
            {
              var delimiter = typeUsage.ScalarTypeName.Delimiter;
              if (delimiter != null)
              {
                var endOffset = delimiter.GetDocumentRange().TextRange.EndOffset;
                if (range.SetEndTo(endOffset) == markerRange) return node;
              }
            }
          }

          var referenceName = node as IReferenceName;
          if (referenceName != null)
          {
            var delimiter = referenceName.Delimiter;
            if (delimiter != null)
            {
              var endOffset = delimiter.GetDocumentRange().TextRange.EndOffset;
              if (range.SetEndTo(endOffset) == markerRange) return node;
            }
          }
        }

        node = node.Parent;
      }

      return null;
    }

    private DocumentRange CalculateRangeToRemove(TextRange nameRange,
      [NotNull] ICSharpExpression expression, [CanBeNull] ITreeNode reference)
    {
      var nameEndOffset = nameRange.EndOffset;
      var replaceOffsetEnd = myReplaceRange.TextRange.EndOffset;

      var replaceRange = (nameEndOffset > replaceOffsetEnd)
        ? myReplaceRange.SetEndTo(nameEndOffset) : myReplaceRange;

      // append semicolon to range if needed
      if (reference is IReferenceExpression && RemoveSemicolon)
      {
        var semicolon = CommonUtils.FindSemicolonAfter(expression, reference);
        if (semicolon != null)
        {
          var endOffset = semicolon.GetDocumentRange().TextRange.EndOffset;
          if (endOffset > replaceRange.TextRange.EndOffset)
            return replaceRange.SetEndTo(endOffset);
        }
      }

      return replaceRange;
    }

    [NotNull] private static ICSharpExpression FixExpression(
      [NotNull] ICSharpExpression expression, [CanBeNull] ITreeNode reference)
    {
      var referenceExpression = reference as IReferenceExpression;
      if (referenceExpression != null)
      {
        var marker = new TreeNodeMarker<IReferenceExpression>(referenceExpression);
        var exprCopy = expression.Copy(expression);
        var refCopy = marker.GetAndDispose(exprCopy);

        var exprToFix = refCopy.QualifierExpression.NotNull();
        LowLevelModificationUtil.ReplaceChildRange(refCopy, refCopy, exprToFix);

        if (exprToFix.NextSibling is IErrorElement)
          LowLevelModificationUtil.DeleteChild(exprToFix.NextSibling);

        return exprCopy;
      }

      var referenceName = reference as IReferenceName;
      if (referenceName != null)
      {
        var marker = new TreeNodeMarker<IReferenceName>(referenceName);
        var exprCopy = expression.Copy(expression);
        var refCopy = marker.GetAndDispose(exprCopy);

        var exprToFix = refCopy.Qualifier.NotNull();
        LowLevelModificationUtil.ReplaceChildRange(refCopy, refCopy, exprToFix);

        if (exprToFix.NextSibling is IErrorElement)
          LowLevelModificationUtil.DeleteChild(exprToFix.NextSibling);

        if (exprToFix.Parent != null && exprToFix.Parent.NextSibling is IErrorElement)
          LowLevelModificationUtil.DeleteChild(exprToFix.Parent.NextSibling);

        return exprCopy;
      }
      else
      {
        var errorsCollector = new RecursiveElementCollector<IErrorElement>();
        expression.ProcessDescendants(errorsCollector);
        var errorElements = errorsCollector.GetResults().Select(
          errorElement => new TreeNodeMarker<IErrorElement>(errorElement)).ToList();

        var exprCopy = expression.Copy(expression);
        foreach (var errorMarker in errorElements)
        {
          var element = errorMarker.GetAndDispose(exprCopy);
          LowLevelModificationUtil.DeleteChild(element);
        }

        return exprCopy;
      }
    }

    protected abstract void ExpandPostfix(
      [NotNull] ITextControl textControl, [NotNull] Suffix suffix,
      [NotNull] ISolution solution, DocumentRange replaceRange,
      [NotNull] IPsiModule psiModule, [NotNull] ICSharpExpression expression);

    protected void AfterComplete(
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

    public IconId Image
    {
      get { return ServicesThemedIcons.LiveTemplate.Id; }
    }

    public RichText DisplayName { get { return myShortcut; } }
    public RichText DisplayTypeName { get { return null; } }
    public string OrderingString { get { return myShortcut; } }
    public string Identity { get { return myShortcut; } }
  }
}