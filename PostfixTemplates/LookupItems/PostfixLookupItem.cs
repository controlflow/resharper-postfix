using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.DataFlow;
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Feature.Services.Resources;
using JetBrains.ReSharper.Feature.Services.Tips;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Services;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Text;
using JetBrains.TextControl;
using JetBrains.UI.Icons;
using JetBrains.UI.RichText;
using JetBrains.Util;
#if RESHARPER9
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.Match;
#endif

namespace JetBrains.ReSharper.PostfixTemplates.LookupItems
{
  public abstract class PostfixLookupItem<TNode> : PostfixLookupItemBase,
    // ReSharper disable RedundantNameQualifier
    JetBrains.ReSharper.Feature.Services.Lookup.ILookupItem
    // ReSharper enable RedundantNameQualifier
    where TNode : class, ITreeNode
  {
    [NotNull] private readonly Lifetime myLifetime;
    [NotNull] private readonly string myShortcut, myIdentifier, myReparseString;
    [NotNull] private readonly ExpressionContextImage[] myImages;
    private int myExpressionIndex = -1;

    protected PostfixLookupItem(
      [NotNull] string shortcut, [NotNull] PrefixExpressionContext context)
      : this(shortcut, new[] {context}) { }

    protected PostfixLookupItem(
      [NotNull] string shortcut, [NotNull] PrefixExpressionContext[] contexts)
    {
      Assertion.Assert(contexts.Length > 0, "contexts.Length > 0");

      myIdentifier = shortcut;
      myShortcut = shortcut.ToLowerInvariant();
      myImages = Array.ConvertAll(contexts, x => new ExpressionContextImage(x));
      myExpressionIndex = (contexts.Length > 1 ? -1 : 0);

      var executionContext = contexts[0].PostfixContext.ExecutionContext;
      myReparseString = executionContext.ReparseString;
      myLifetime = executionContext.Lifetime;
    }

    public virtual MatchingResult Match(string prefix, ITextControl textControl)
    {
      return LookupUtil.MatchPrefix(new IdentifierMatcher(prefix), myIdentifier);
    }

    protected string ExpandCommandName
    {
      get { return GetType().FullName + " expansion"; }
    }

    protected virtual string ExpressionSelectTitle
    {
      get { return "Select expression"; }
    }

    [NotNull] protected Lifetime Lifetime
    {
      get { return myLifetime; }
    }

    public void Accept(ITextControl textControl, TextRange nameRange,
                       LookupItemInsertType insertType, Suffix suffix,
                       ISolution solution, bool keepCaretStill)
    {
      textControl.Document.InsertText(
        nameRange.EndOffset, myReparseString, TextModificationSide.RightSide);

      solution.GetPsiServices().CommitAllDocuments();

      var itemsOwnerFactory = solution.GetComponent<LookupItemsOwnerFactory>();
      var templatesManager = solution.GetComponent<PostfixTemplatesManager>();
      var lookupItemsOwner = itemsOwnerFactory.CreateLookupItemsOwner(textControl);

      PostfixTemplateContext postfixContext = null;
      var identifierOffset = (textControl.Caret.Offset() - myReparseString.Length);

      foreach (var position in TextControlToPsi
        .GetElements<ITokenNode>(solution, textControl.Document, identifierOffset))
      {
        var executionContext = new PostfixExecutionContext(
          myLifetime, solution, textControl, lookupItemsOwner, myReparseString, false);

        postfixContext = templatesManager.IsAvailable(position, executionContext);
        if (postfixContext != null) break;
      }

      TipsManager.Instance.FeatureIsUsed(
        "Plugin.ControlFlow.PostfixTemplates." + myShortcut, textControl.Document, solution);

      Assertion.AssertNotNull(postfixContext, "postfixContext != null");

      var expressions = FindOriginalContexts(postfixContext);
      Assertion.Assert(expressions.Count > 0, "expressions.Count > 0");

      if (expressions.Count > 1 && myExpressionIndex == -1)
      {
        // rollback document changes to hide reparse string from user
        var chooser = solution.GetComponent<ExpressionChooser>();

        var postfixRange = GetPostfixRange(textControl, nameRange);
        var postfixText = textControl.Document.GetText(postfixRange);
        textControl.Document.ReplaceText(postfixRange, string.Empty);

        chooser.Execute(myLifetime, textControl, expressions, postfixText, ExpressionSelectTitle, index =>
        {
          myExpressionIndex = index;

          // yep, run accept recursively, now with selected item index
          var locks = solution.GetComponent<IShellLocks>();
          locks.ReentrancyGuard.ExecuteOrQueue("PostfixTemplates.Accept", () =>
          {
            locks.ExecuteWithReadLock(() =>
            {
              var text = postfixText.Substring(0, postfixText.Length - myReparseString.Length);
              textControl.Document.InsertText( // bring back ".name__"
                postfixRange.StartOffset, text, TextModificationSide.RightSide);

              Accept(textControl, nameRange, insertType, suffix, solution, keepCaretStill);
            });
          });
        });
        return;
      }

      Assertion.Assert(myExpressionIndex >= 0, "myExpressionIndex >= 0");
      Assertion.Assert(myExpressionIndex < expressions.Count, "myExpressionIndex < expressions.Count");
      var expressionContext = expressions[myExpressionIndex];

      TNode newNode;
      using (WriteLockCookie.Create())
      {
        var fixedContext = postfixContext.FixExpression(expressionContext);

        var expression = fixedContext.Expression;
        Assertion.Assert(expression.IsPhysical(), "expression.IsPhysical()");

        newNode = ExpandPostfix(fixedContext);
        Assertion.AssertNotNull(newNode, "newNode != null");
        Assertion.Assert(newNode.IsPhysical(), "newNode.IsPhysical()");
      }

      AfterComplete(textControl, newNode);
    }

    private TextRange GetPostfixRange([NotNull] ITextControl textControl, TextRange nameRange)
    {
      Assertion.Assert(nameRange.IsValid, "nameRange.IsValid");

      var length = nameRange.Length + myReparseString.Length;
      var textRange = TextRange.FromLength(nameRange.StartOffset, length);

      // find dot before postfix template name
      var buffer = textControl.Document.Buffer;
      for (var index = nameRange.StartOffset - 1; index > 0; index--)
      {
        if (buffer[index] == '.') return textRange.SetStartTo(index);
      }

      return textRange;
    }

    protected abstract TNode ExpandPostfix([NotNull] PrefixExpressionContext context);

    protected virtual void AfterComplete([NotNull] ITextControl textControl, [NotNull] TNode node) { }

    [NotNull]
    private IList<PrefixExpressionContext> FindOriginalContexts([NotNull] PostfixTemplateContext context)
    {
      var results = new LocalList<PrefixExpressionContext>();
      var images = new List<ExpressionContextImage>(myImages);

      for (var index = 0; index < images.Count; index++) // order is important
      {
        foreach (var expressionContext in context.ExpressionsOrTypes)
        {
          if (images[index].MatchesByRangeAndType(expressionContext))
          {
            images[index] = null;
            results.Add(expressionContext);
            break;
          }
        }
      }

      if (results.Count == 0)
      {
        var expressions = context.Expressions;
        foreach (var image in myImages)
        {
          if (image != null && image.ContextIndex < expressions.Count)
          {
            results.Add(expressions[image.ContextIndex]);
          }
        }
      }

      return results.ResultingList();
    }

    public IconId Image
    {
      get { return ServicesThemedIcons.LiveTemplate.Id; }
    }

    public RichText DisplayName { get { return myShortcut; } }
    public virtual RichText DisplayTypeName { get { return null; } }
    public string Identity { get { return myShortcut; } }

    public string OrderingString
    {
      get { return myShortcut; }
      set { }
    }

    private sealed class ExpressionContextImage
    {
      [NotNull] private readonly Type myExpressionType;
      private readonly DocumentRange myExpressionRange;
      private readonly int myContextIndex;

      public ExpressionContextImage([NotNull] PrefixExpressionContext context)
      {
        myExpressionType = context.Expression.GetType();
        myExpressionRange = context.ExpressionRange;
        myContextIndex = context.PostfixContext.Expressions.IndexOf(context);
      }

      public int ContextIndex
      {
        get { return myContextIndex; }
      }

      public bool MatchesByRangeAndType([NotNull] PrefixExpressionContext context)
      {
        var startOffset = myExpressionRange.TextRange.StartOffset;
        return context.Expression.GetType() == myExpressionType
            && context.ExpressionRange.TextRange.StartOffset == startOffset;
      }
    }
  }
}