using System;
using System.Collections.Generic;
using JetBrains.ActionManagement;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.CommonControls;
using JetBrains.DataFlow;
using JetBrains.DocumentModel;
using JetBrains.IDE;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Hotspots;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Feature.Services.Resources;
using JetBrains.ReSharper.Feature.Services.Tips;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Services;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Text;
using JetBrains.TextControl;
using JetBrains.TextControl.DocumentMarkup;
using JetBrains.Threading;
using JetBrains.UI.Icons;
using JetBrains.UI.PopupMenu;
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
    private int mySelectedExprIndex = -1;

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
      mySelectedExprIndex = (contexts.Length > 1 ? -1 : 0);

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
      Assertion.AssertNotNull(expressions.Count > 0, "expressions.Count > 0");

      if (expressions.Count > 1 && mySelectedExprIndex == -1)
      {
        ShowExpressionChooser(expressions, textControl, solution, suffix, nameRange, insertType, keepCaretStill);
        return;
      }

      Assertion.Assert(mySelectedExprIndex >= 0, "mySelectedExprIndex >= 0");
      Assertion.Assert(mySelectedExprIndex < expressions.Count, "mySelectedExprIndex < expressions.Count");
      var expressionContext = expressions[mySelectedExprIndex];

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

    protected abstract TNode ExpandPostfix([NotNull] PrefixExpressionContext context);
    protected virtual void AfterComplete([NotNull] ITextControl textControl, [NotNull] TNode node) { }

    [NotNull]
    private IList<PrefixExpressionContext> FindOriginalContexts([NotNull] PostfixTemplateContext context)
    {
      var results = new LocalList<PrefixExpressionContext>();
      var images = new HashSet<ExpressionContextImage>(myImages);

      foreach (var expressionContext in context.ExpressionsOrTypes)
      {
        foreach (var contextImage in images)
        {
          if (contextImage.MatchesByRangeAndType(expressionContext))
          {
            images.Remove(contextImage);
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
          if (image.ContextIndex < expressions.Count)
            results.Add(expressions[image.ContextIndex]);
        }
      }

      return results.ResultingList();
    }

    private void ShowExpressionChooser([NotNull] IList<PrefixExpressionContext> expressions,
                                       [NotNull] ITextControl textControl, [NotNull] ISolution solution,
                                       [NotNull] Suffix suffix, TextRange nameRange,
                                       LookupItemInsertType insertType, bool keepCaretStill)
    {
      var menu = Shell.Instance.GetComponent<JetPopupMenus>().Create();
      menu.Caption.Value = WindowlessControl.Create("Select expression");


      menu.PopupWindowContext = new TextControlPopupWindowContext(
        myLifetime, textControl, // todo: caret pos?
        Shell.Instance.GetComponent<IShellLocks>(),
        Shell.Instance.GetComponent<IActionManager>());

      // todo: restore selection
      // todo: handle escape action

      var key = new Key("aa");

      menu.SelectedItemKey.Change.Advise(myLifetime, x =>
      {
        var simpleMenuItem = x.New as SimpleMenuItem;
        if (simpleMenuItem != null)
        {
          var range = (TextRange) simpleMenuItem.Tag;
          Shell.Instance.GetComponent<IThreading>().ExecuteOrQueue("aa" , () =>
          {
            using (ReadLockCookie.Create())
            {
              var manager = Shell.Instance.GetComponent<IDocumentMarkupManager>();
              var documentMarkup = manager.GetMarkupModel(textControl.Document);

              foreach (var highlighter in documentMarkup.GetHighlightersEnumerable(key))
              {
                documentMarkup.RemoveHighlighter(highlighter);
                break;
              }

              documentMarkup.AddHighlighter(key, range, AreaType.EXACT_RANGE, 0,
                HotspotSessionUi.CURRENT_HOTSPOT_HIGHLIGHTER, ErrorStripeAttributes.Empty, null, null);
            }


          });
        }
      });

      // rollback changes :(
      textControl.Document.ReplaceText(
        TextRange.FromLength(nameRange.EndOffset, myReparseString.Length), string.Empty);

      var items = new LocalList<SimpleMenuItem>(expressions.Count);
      int index = 0;

      foreach (var context in expressions)
      {
        var text = context.Expression.GetText();

        if (context.Expression.Contains(context.PostfixContext.Reference))
        {
          var postfix = myShortcut + myReparseString;
          if (text.EndsWith(postfix, StringComparison.OrdinalIgnoreCase))
          {
            text = text.Substring(0, text.Length - postfix.Length).TrimEnd();
          }

          if (text.EndsWith(".", StringComparison.Ordinal))
          {
            text = text.Substring(0, text.Length - 1).TrimEnd();
          }
        }

        text = text.ReplaceNewLines(string.Empty).TrimStart();

        // todo: overflow

        int index1 = index;
        var menuItem = new SimpleMenuItem(
          text: text,
          icon: BulbThemedIcons.ContextAction.Id,
          FOnExecute: () =>
          {
            mySelectedExprIndex = index1;

            Accept(textControl, nameRange, insertType, suffix, solution, keepCaretStill);

          });

        menuItem.Tag = context.ExpressionRange.TextRange;

        items.Add(menuItem);
        index++;
      }

      menu.SetItems(items.ToArray());
      menu.Show(JetPopupMenu.ShowWhen.AutoExecuteIfSingleItem);
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