using System.Linq;
using JetBrains.ActionManagement;
using JetBrains.Annotations;
using JetBrains.Application.CommandProcessing;
using JetBrains.Application.DataContext;
using JetBrains.DataFlow;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.LiveTemplates;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Services;
using JetBrains.TextControl;
using JetBrains.TextControl.Actions;
using JetBrains.TextControl.Util;
using JetBrains.Util;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion
{
  [SolutionComponent]
  public sealed class PostfixTemplatesTracker
  {
    public PostfixTemplatesTracker(
      [NotNull] Lifetime lifetime, [NotNull] PostfixTemplatesManager templatesManager,
      [NotNull] IActionManager manager, [NotNull] TextControlChangeUnitFactory changeUnitFactory,
      [NotNull] ILookupWindowManager lookupWindowManager, [NotNull] ICommandProcessor processor,
      [NotNull] LookupItemsOwnerFactory lookupItemsOwnerFactory)
    {
      // override livetemplates expand action
      var expandAction = manager.TryGetAction(TextControlActions.TAB_ACTION_ID) as IUpdatableAction;
      if (expandAction != null)
      {
        var postfixHandler = new ExpandPostfixTemplateHandler(
          changeUnitFactory, templatesManager, lookupWindowManager, processor, lookupItemsOwnerFactory);
        expandAction.AddHandler(lifetime, postfixHandler);
      }
    }

    private sealed class ExpandPostfixTemplateHandler : IActionHandler
    {
      [NotNull] private readonly ILookupWindowManager myLookupWindowManager;
      [NotNull] private readonly TextControlChangeUnitFactory myChangeUnitFactory;
      [NotNull] private readonly PostfixTemplatesManager myTemplatesManager;
      [NotNull] private readonly ICommandProcessor myCommandProcessor;
      [NotNull] private readonly LookupItemsOwnerFactory myItemsOwnerFactory;

      public ExpandPostfixTemplateHandler(
        [NotNull] TextControlChangeUnitFactory changeUnitFactory,
        [NotNull] PostfixTemplatesManager templatesManager,
        [NotNull] ILookupWindowManager lookupWindowManager,
        [NotNull] ICommandProcessor commandProcessor,
        [NotNull] LookupItemsOwnerFactory itemsOwnerFactory)
      {
        myChangeUnitFactory = changeUnitFactory;
        myLookupWindowManager = lookupWindowManager;
        myCommandProcessor = commandProcessor;
        myItemsOwnerFactory = itemsOwnerFactory;
        myTemplatesManager = templatesManager;
      }

      public bool Update(IDataContext context, ActionPresentation presentation, DelegateUpdate nextUpdate)
      {
        var solution = context.GetData(ProjectModel.DataContext.DataConstants.SOLUTION);
        var textControl = context.GetData(TextControl.DataContext.DataConstants.TEXT_CONTROL);
        if (textControl != null && solution != null)
        {
          var sourceFile = textControl.Document.GetPsiSourceFile(solution);
          if (sourceFile != null)
          {
            var template = GetTemplateFromTextControl(textControl, solution, isExecuting: false);
            if (template != null) return true;
          }
        }

        return nextUpdate();
      }

      public void Execute(IDataContext context, DelegateExecute nextExecute)
      {
        var solution = context.GetData(ProjectModel.DataContext.DataConstants.SOLUTION);
        var textControl = context.GetData(TextControl.DataContext.DataConstants.TEXT_CONTROL);
        if (textControl != null && solution != null && myLookupWindowManager.CurrentLookup == null)
        {
          const string commandName = "Expand postfix template";
          var updateCookie = myChangeUnitFactory.CreateChangeUnit(textControl, commandName);
          try
          {
            using (myCommandProcessor.UsingCommand(commandName))
            {
              var template = GetTemplateFromTextControl(textControl, solution, isExecuting: true);
              if (template != null)
              {
                var nameLength = template.Identity.Length;
                var offset = textControl.Caret.Offset() - nameLength;
                var nameRange = TextRange.FromLength(offset, nameLength);

                // invoke item completion manually
                template.Accept(
                  textControl, nameRange, LookupItemInsertType.Replace,
                  Suffix.Empty, solution, false);

                return;
              }

              updateCookie.Dispose();
            }
          }
          catch
          {
            updateCookie.Dispose();
            throw;
          }
        }

        nextExecute();
      }

      [CanBeNull] private ILookupItem GetTemplateFromTextControl(
        [NotNull] ITextControl textControl, [NotNull] ISolution solution, bool isExecuting)
      {
        var caretOffset = textControl.Caret.Offset();
        var genericPrefix = LiveTemplatesManager.GetPrefix(textControl.Document, caretOffset);

        // fast check
        // todo: less ugly plz!
        if (myTemplatesManager.TemplateProvidersInfos.All(x => x.Metadata.TemplateName != genericPrefix))
          return null;

        var rollback = true;
        try
        {
          textControl.Document.InsertText(caretOffset, "__");
          solution.GetPsiServices().CommitAllDocuments();

          var token = TextControlToPsi.GetSourceTokenBeforeCaret(solution, textControl);
          if (token == null) return null;

          var lookupItemsOwner = myItemsOwnerFactory.CreateLookupItemsOwner(textControl);
          var executionContext = new PostfixExecutionContext(
            true, token.GetPsiModule(), lookupItemsOwner, reparseString: genericPrefix);

          var items = myTemplatesManager.GetAvailableItems(token, executionContext, templateName: genericPrefix);
          if (items.Count != 1) return null;

          if (isExecuting) rollback = false;
          return items[0];
        }
        finally
        {
          if (rollback)
            textControl.Document.DeleteText(TextRange.FromLength(caretOffset, 2));
        }
      }
    }
  }
}