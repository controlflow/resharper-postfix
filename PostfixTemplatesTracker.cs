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
      [NotNull] ILookupWindowManager lookupWindowManager, [NotNull] ICommandProcessor processor)
    {
      // override livetemplates expand action
      var expandLiveTemplateAction = manager.TryGetAction(TextControlActions.TAB_ACTION_ID) as IUpdatableAction;
      if (expandLiveTemplateAction != null)
      {
        expandLiveTemplateAction.AddHandler(lifetime, new ExpandPostfixTemplateHandler(
          changeUnitFactory, templatesManager, lookupWindowManager, processor));
      }
    }

    public sealed class ExpandPostfixTemplateHandler : IActionHandler
    {
      [NotNull] private readonly ILookupWindowManager myLookupWindowManager;
      [NotNull] private readonly TextControlChangeUnitFactory myChangeUnitFactory;
      [NotNull] private readonly PostfixTemplatesManager myTemplatesManager;
      [NotNull] private readonly ICommandProcessor myCommandProcessor;

      public ExpandPostfixTemplateHandler(
        [NotNull] TextControlChangeUnitFactory changeUnitFactory,
        [NotNull] PostfixTemplatesManager templatesManager,
        [NotNull] ILookupWindowManager lookupWindowManager,
        [NotNull] ICommandProcessor commandProcessor)
      {
        myChangeUnitFactory = changeUnitFactory;
        myLookupWindowManager = lookupWindowManager;
        myCommandProcessor = commandProcessor;
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
            var template = GetTemplateFromTextControl(textControl, solution);
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
              var template = GetTemplateFromTextControl(textControl, solution);
              if (template != null)
              {
                var nameLength = template.Identity.Length;
                var nameRange = TextRange.FromLength(
                  textControl.Caret.Offset() - nameLength, nameLength);

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

      [CanBeNull]
      private ILookupItem GetTemplateFromTextControl(
        [NotNull] ITextControl textControl, [NotNull] ISolution solution)
      {
        solution.GetPsiServices().CommitAllDocuments();

        var caretOffset = textControl.Caret.Offset();
        var genericPrefix = LiveTemplatesManager.GetPrefix(textControl.Document, caretOffset);

        var token = TextControlToPsi.GetSourceTokenBeforeCaret(solution, textControl);
        if (token != null)
        {
          // check exactly single item available
          var items = myTemplatesManager.GetAvailableItems(
            token, forceMode: true, templateName: genericPrefix);

          if (items.Count == 1) return items[0];
        }

        return null;
      }
    }
  }
}