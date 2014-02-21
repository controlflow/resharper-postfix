using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.CommonControls;
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

    protected PostfixLookupItem(
      [NotNull] string shortcut, [NotNull] PrefixExpressionContext context)
      : this(shortcut, new[] { context }) { }

    protected PostfixLookupItem(
      [NotNull] string shortcut, [NotNull] PrefixExpressionContext[] contexts)
    {
      Assertion.Assert(contexts.Length > 0, "contexts.Length > 0");

      myIdentifier = shortcut;
      myShortcut = shortcut.ToLowerInvariant();
      myImages = Array.ConvertAll(contexts, x => new ExpressionContextImage(x));

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

      var expressionContexts = FindOriginalContexts(postfixContext);
      Assertion.AssertNotNull(expressionContexts.Count > 0, "expressionContexts.Count > 0");

      ChooseExpression(expressionContexts, expressionContext =>
      {
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
      });
    }

    protected abstract TNode ExpandPostfix([NotNull] PrefixExpressionContext context);
    protected virtual void AfterComplete([NotNull] ITextControl textControl, [NotNull] TNode node) { }

    [NotNull]
    private IList<PrefixExpressionContext> FindOriginalContexts([NotNull] PostfixTemplateContext context)
    {
      var results = new LocalList<PrefixExpressionContext>();
      var images = myImages;

      foreach (var expressionContext in context.ExpressionsOrTypes)
      {
        for (var index = 0; index < images.Length; index++)
        {
          var image = images[index];
          if (image != null && image.MatchesByRangeAndType(expressionContext))
          {
            results.Add(expressionContext);
            images[index] = null;
          }
        }
      }

      if (results.Count <= 0)
      {
        var expressions = context.Expressions;
        foreach (var image in myImages)
        {
          if (image.ContextIndex < expressions.Count)
          {
            results.Add(expressions[image.ContextIndex]);
          }
        }
      }

      return results.ResultingList();
    }

    private static void ChooseExpression(
      [NotNull] IList<PrefixExpressionContext> expressions,
      [NotNull] Action<PrefixExpressionContext> continuation)
    {
      if (expressions.Count > 0)
      {
        var menu = Shell.Instance.GetComponent<JetPopupMenus>().Create();
        //Shell.Instance.GetComponent<>()

        menu.Caption.Value = WindowlessControl.Create("Select target expression");

        var items = new LocalList<SimpleMenuItem>(expressions.Count);
        foreach (var context in expressions)
        {
          var text = context.Expression.GetText();
          text = text.ReplaceNewLines(string.Empty);

          var context1 = context;
          var menuItem = new SimpleMenuItem(
            text: text,
            icon: BulbThemedIcons.YellowBulb.Id,
            FOnExecute: () => continuation(context1));

          items.Add(menuItem);
        }

        menu.SetItems(items.ToArray());
        menu.ShowModal(JetPopupMenu.ShowWhen.AutoExecuteIfSingleItem);

        Console.WriteLine("LOL");
      }
      else
      {
        continuation(expressions[0]);
      }
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