using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.Application.CommandProcessing;
using JetBrains.DataFlow;
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.BaseInfrastructure;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Feature.Services.Tips;
using JetBrains.ReSharper.Feature.Services.Util;
using JetBrains.ReSharper.PostfixTemplates.Contexts;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.PostfixTemplates.LookupItems
{
  public abstract class PostfixTemplateBehavior : LookupItemAspect<PostfixTemplateInfo>, ILookupItemBehavior
  {
    private int myExpressionIndex;

    protected PostfixTemplateBehavior([NotNull] PostfixTemplateInfo info)
      : base(info)
    {
      myExpressionIndex = (info.Images.Count > 1 ? -1 : 0);
    }

    protected string ExpandCommandName
    {
      get { return GetType().FullName + " accept"; }
    }

    protected virtual string ExpressionSelectTitle
    {
      get { return "Select expression"; }
    }


    public bool AcceptIfOnlyMatched(LookupItemAcceptanceContext itemAcceptanceContext)
    {
      return false;
    }

    public void Accept(ITextControl textControl, TextRange nameRange, LookupItemInsertType insertType,
                       Suffix suffix, ISolution solution, bool keepCaretStill)
    {
      // todo: carefully review and document all of this :\
      var reparseString = Info.ReparseString;

      // so we inserted '.__' and get '2 + 2.__' just like in code completion reparse
      textControl.Document.InsertText(nameRange.EndOffset, reparseString, TextModificationSide.RightSide);

      solution.GetPsiServices().Files.CommitAllDocuments();

      var offset = nameRange.EndOffset + reparseString.Length;
      var execContext = new PostfixTemplateExecutionContext(
        solution, textControl, Info.ExecutionContext.SettingsStore, Info.ReparseString, Info.ExecutionContext.IsPreciseMode);

      PostfixTemplateContext postfixContext = null;

      foreach (var element in TextControlToPsi.GetElements<ITreeNode>(solution, textControl.Document, offset))
      {
        var contextFactory = LanguageManager.Instance.TryGetService<IPostfixTemplateContextFactory>(element.Language);
        if (contextFactory == null) continue;

        postfixContext = contextFactory.TryCreate(element, execContext);
        if (postfixContext != null) break;
      }

      // todo: [R#] good feature id, looks at source templates 'Accept()'
      TipsManager.Instance.FeatureIsUsed(
        "Plugin.ControlFlow.PostfixTemplates." + Info.Text, textControl.Document, solution);

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

        chooser.Execute(
          EternalLifetime.Instance, textControl, expressions, postfixText,
          ExpressionSelectTitle, continuation: index =>
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
                  var text = postfixText.Substring(0, postfixText.Length - reparseString.Length);

                  // todo: don't like it very much, is there a better way to solve this?
                  textControl.Document.InsertText( // bring back ".name__"
                    postfixRange.StartOffset, text, TextModificationSide.RightSide);

                  // ah!
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

      ITreeNode newNode;
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

      var length = nameRange.Length + Info.ReparseString.Length;
      var textRange = TextRange.FromLength(nameRange.StartOffset, length);

      // find dot before postfix template name
      var buffer = textControl.Document.Buffer;
      for (var index = nameRange.StartOffset - 1; index > 0; index--)
      {
        if (buffer[index] == '.') return textRange.SetStartTo(index);
      }

      return textRange;
    }

    protected abstract ITreeNode ExpandPostfix([NotNull] PostfixExpressionContext context);

    protected virtual void AfterComplete([NotNull] ITextControl textControl, [NotNull] ITreeNode node)
    {
      
    }

    [NotNull]
    private IList<PostfixExpressionContext> FindOriginalContexts([NotNull] PostfixTemplateContext context)
    {
      var results = new LocalList<PostfixExpressionContext>();
      var images = new List<PostfixExpressionContextImage>(Info.Images);

      for (var index = 0; index < images.Count; index++) // order is important
      {
        foreach (var expressionContext in context.AllExpressions)
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
        var expressions = context.AllExpressions;

        foreach (var image in images)
        {
          if (image != null && image.ExpressionIndex < expressions.Count)
          {
            results.Add(expressions[image.ExpressionIndex]);
          }
        }
      }

      return results.ResultingList();
    }
  }
}