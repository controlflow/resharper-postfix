using System;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Feature.Services.Resources;
using JetBrains.ReSharper.I18n.Services.Refactoring;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Services;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Text;
using JetBrains.TextControl;
using JetBrains.UI.Icons;
using JetBrains.UI.RichText;
using JetBrains.Util;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.Util.Logging;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems
{
  public abstract class PostfixLookupItem<TNode> : PostfixLookupItemBase, ILookupItem
    where TNode : class, ITreeNode
  {
    [NotNull] private readonly string myShortcut;
    [NotNull] private readonly string myIdentifier;
    [NotNull] private readonly string myReparseString;
    [NotNull] private readonly Type myExpressionType;
    private readonly DocumentRange myExpressionRange;
    private readonly int myContextIndex;

    protected PostfixLookupItem(
      [NotNull] string shortcut, [NotNull] PrefixExpressionContext context)
    {
      myIdentifier = shortcut;
      myShortcut = shortcut.ToLowerInvariant();
      myExpressionType = context.Expression.GetType();
      myExpressionRange = context.ExpressionRange;
      myContextIndex = context.Parent.Expressions.IndexOf(context);
      myReparseString = context.Parent.ExecutionContext.ReparseString;
    }

    public MatchingResult Match(string prefix, ITextControl textControl)
    {
      return LookupUtil.MatchPrefix(new IdentifierMatcher(prefix), myIdentifier);
    }

    public void Accept(
      ITextControl textControl, TextRange nameRange, LookupItemInsertType insertType,
      Suffix suffix, ISolution solution, bool keepCaretStill)
    {
      textControl.Document.InsertText(
        nameRange.EndOffset, myReparseString, TextModificationSide.RightSide);

      solution.GetPsiServices().Files.CommitAllDocuments();

      var position = TextControlToPsi.GetElementFromCaretPosition<ITreeNode>(solution, textControl);

      var itemsOwnerFactory = solution.GetComponent<LookupItemsOwnerFactory>();
      var lookupItemsOwner = itemsOwnerFactory.CreateLookupItemsOwner(textControl);
      var templatesManager = solution.GetComponent<PostfixTemplatesManager>();

      var psiModule = position.GetPsiModule();
      var executionContext = new PostfixExecutionContext(
        false, psiModule, lookupItemsOwner, myReparseString);

      var postfixContext = templatesManager.IsAvailable(position, executionContext);
      Assertion.AssertNotNull(postfixContext, "postfixContext != null");

      var expressionContext = FindOriginalContext(postfixContext);
      Assertion.AssertNotNull(expressionContext, "expressionContext != null");

      ITreeNodePointer<TNode> pointer = null;
      using (WriteLockCookie.Create())
      {
        var commandName = GetType().FullName + " expansion";
        solution.GetPsiServices().DoTransaction(commandName, () =>
        {
          var fixedContext = postfixContext.FixExpression(expressionContext);

          var expression = fixedContext.Expression;
          Assertion.Assert(expression.IsPhysical(), "expression.IsPhysical()");

          var newNode = ExpandPostfix(fixedContext);
          Assertion.AssertNotNull(newNode, "newNode != null");
          Assertion.Assert(newNode.IsPhysical(), "newNode.IsPhysical()");

          pointer = newNode.CreatePointer();
        });
      }

      if (pointer != null)
      {
        var newNode = pointer.GetTreeNode();
        if (newNode != null) AfterComplete(textControl, newNode);
      }
    }

    protected abstract TNode ExpandPostfix([NotNull] PrefixExpressionContext expression);

    protected virtual void AfterComplete([NotNull] ITextControl textControl, [NotNull] TNode node)
    {

    }

    [CanBeNull]
    private PrefixExpressionContext FindOriginalContext([NotNull] PostfixTemplateContext context)
    {
      var startOffset = myExpressionRange.TextRange.StartOffset;
      foreach (var expressionContext in context.Expressions)
      {
        if (expressionContext.Expression.GetType() == myExpressionType &&
            expressionContext.ExpressionRange.TextRange.StartOffset == startOffset)
        {
          return expressionContext;
        }
      }

      if (context.Expressions.Count < myContextIndex)
      {
        return context.Expressions[myContextIndex];
      }

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