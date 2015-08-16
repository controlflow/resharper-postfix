using System;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.Application.CommandProcessing;
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.BaseInfrastructure;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Feature.Services.Tips;
using JetBrains.ReSharper.Feature.Services.Util;
using JetBrains.ReSharper.PostfixTemplates.CodeCompletion;
using JetBrains.ReSharper.PostfixTemplates.Contexts;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.PostfixTemplates.LookupItems
{
  public abstract class PostfixTemplateBehaviorBase : LookupItemAspect<PostfixTemplateInfo>, ILookupItemBehavior
  {
    protected PostfixTemplateBehaviorBase([NotNull] PostfixTemplateInfo info) : base(info) { }

    public bool AcceptIfOnlyMatched(LookupItemAcceptanceContext itemAcceptanceContext)
    {
      return false;
    }

    public void Accept(ITextControl textControl, TextRange nameRange, LookupItemInsertType lookupItemInsertType, Suffix suffix, ISolution solution, bool keepCaretStill)
    {
      // insert the same reparsed text first
      textControl.Document.InsertText(nameRange.EndOffset, Info.ReparseString, TextModificationSide.RightSide);

      solution.GetPsiServices().CommitAllDocuments();

      var templatesManager = solution.GetComponent<PostfixTemplatesManager>();

      PostfixTemplateContext postfixContext = null;
      var identifierOffset = (textControl.Caret.Offset() - Info.ReparseString.Length);

      foreach (var position in TextControlToPsi.GetElements<ITokenNode>(solution, textControl.Document, identifierOffset))
      {
        var executionContext = new PostfixExecutionContext(myLifetime, solution, textControl, Info.ReparseString, false);

        postfixContext = templatesManager.IsAvailable(position, executionContext);
        if (postfixContext != null) break;
      }

      TipsManager.Instance.FeatureIsUsed("Plugin.ControlFlow.PostfixTemplates." + Info.Shortcut, textControl.Document, solution);

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

        chooser.Execute(myLifetime, textControl, expressions,
                        postfixText, ExpressionSelectTitle, index =>
                        {
                          myExpressionIndex = index;

                          // yep, run accept recursively, now with selected item index
                          var locks = solution.GetComponent<IShellLocks>();
                          const string commandName = "PostfixTemplates.Accept";

                          locks.ReentrancyGuard.ExecuteOrQueue(commandName, () =>
                          {
                            locks.ExecuteWithReadLock(() =>
                            {
                              var processor = solution.GetComponent<ICommandProcessor>();
                              using (processor.UsingCommand(commandName))
                              {
                                var text = postfixText.Substring(0, postfixText.Length - myReparseString.Length);
                                textControl.Document.InsertText( // bring back ".name__"
                                  postfixRange.StartOffset, text, TextModificationSide.RightSide);

                                Accept(textControl, nameRange, insertType, suffix, solution, keepCaretStill);
                              }
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
  }
}