using System;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.DataFlow;
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Feature.Services.Resources;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Services;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Text;
using JetBrains.TextControl;
using JetBrains.UI.Icons;
using JetBrains.UI.RichText;
using JetBrains.Util;

namespace JetBrains.ReSharper.PostfixTemplates.LookupItems
{
  public abstract class PostfixLookupItem<TNode> : PostfixLookupItemBase, ILookupItem
    where TNode : class, ITreeNode
  {
    [NotNull] private readonly Lifetime myLifetime;
    [NotNull] private readonly string myShortcut, myIdentifier, myReparseString;
    [NotNull] private readonly Type myExpressionType;
    private readonly DocumentRange myExpressionRange;
    private readonly int myContextIndex;

    protected PostfixLookupItem(
      [NotNull] string shortcut, [NotNull] PrefixExpressionContext context)
    {
      var postfixContext = context.PostfixContext;

      myIdentifier = shortcut;
      myShortcut = shortcut.ToLowerInvariant();
      myExpressionType = context.Expression.GetType();
      myExpressionRange = context.ExpressionRange;
      myContextIndex = postfixContext.Expressions.IndexOf(context);
      myReparseString = postfixContext.ExecutionContext.ReparseString;
      myLifetime = postfixContext.ExecutionContext.Lifetime;
    }

    public MatchingResult Match(string prefix, ITextControl textControl)
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

      Assertion.AssertNotNull(postfixContext, "postfixContext != null");

      var expressionContext = FindOriginalContext(postfixContext);
      Assertion.AssertNotNull(expressionContext, "expressionContext != null");

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

    [CanBeNull]
    private PrefixExpressionContext FindOriginalContext([NotNull] PostfixTemplateContext context)
    {
      var startOffset = myExpressionRange.TextRange.StartOffset;
      foreach (var expressionContext in context.Expressions)
      {
        if (expressionContext.Expression.GetType() == myExpressionType &&
            expressionContext.ExpressionRange.TextRange.StartOffset == startOffset)
          return expressionContext;
      }

      if (context.Expressions.Count < myContextIndex)
        return context.Expressions[myContextIndex];

      return null;
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