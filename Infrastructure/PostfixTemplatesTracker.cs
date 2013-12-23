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
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;
using JetBrains.TextControl.Actions;
using JetBrains.TextControl.Util;
using JetBrains.Util;

namespace JetBrains.ReSharper.PostfixTemplates
{
  [SolutionComponent]
  public sealed class PostfixTemplatesTracker
  {
    public PostfixTemplatesTracker(
      [NotNull] Lifetime lifetime, [NotNull] PostfixTemplatesManager templatesManager,
      [NotNull] IActionManager manager, [NotNull] TextControlChangeUnitFactory changeUnitFactory,
      [NotNull] ILookupWindowManager lookupWindowManager, [NotNull] ICommandProcessor processor,
      [NotNull] LookupItemsOwnerFactory lookupItemsFactory)
    {
      // override livetemplates expand action
      var expandAction = manager.TryGetAction(TextControlActions.TAB_ACTION_ID) as IUpdatableAction;
      if (expandAction != null)
      {
        var postfixHandler = new ExpandPostfixTemplateHandler(
          changeUnitFactory, templatesManager, lookupWindowManager, processor, lookupItemsFactory);
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
          const string commandName = "Expanding postfix template";
          var updateCookie = myChangeUnitFactory.CreateChangeUnit(textControl, commandName);
          try
          {
            using (myCommandProcessor.UsingCommand(commandName))
            {
              var postfixItem = GetTemplateFromTextControl(textControl, solution);
              if (postfixItem != null)
              {
                var nameLength = postfixItem.Identity.Length;
                var offset = textControl.Caret.Offset() - nameLength;
                var nameRange = TextRange.FromLength(offset, nameLength);

                postfixItem.Accept(textControl, nameRange,
                  LookupItemInsertType.Insert, Suffix.Empty, solution, false);

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
        [NotNull] ITextControl textControl, [NotNull] ISolution solution)
      {
        var offset = textControl.Caret.Offset();
        var prefix = LiveTemplatesManager.GetPrefix(textControl.Document, offset);

        if (!TemplateWithNameExists(prefix)) return null;

        return TryReparseWith(textControl, solution, prefix, "__")
            ?? TryReparseWith(textControl, solution, prefix, "__;");
      }

      [CanBeNull] private ILookupItem TryReparseWith(
        [NotNull] ITextControl textControl, [NotNull] ISolution solution,
        [NotNull] string prefix, [NotNull] string reparseString)
      {
        var offset = textControl.Caret.Offset();
        var document = textControl.Document;

        try
        {
          document.InsertText(offset, reparseString);
          solution.GetPsiServices().CommitAllDocuments();
          ILookupItemsOwner itemsOwner = null;

          foreach (var position in
            TextControlToPsi.GetElements<ITokenNode>(solution, document, offset))
          {
            itemsOwner = itemsOwner ?? myItemsOwnerFactory.CreateLookupItemsOwner(textControl);
            var context = new PostfixExecutionContext(
              true, position.GetPsiModule(), itemsOwner, reparseString);

            var postfixItems = myTemplatesManager.GetAvailableItems(position, context, prefix);
            if (postfixItems.Count == 1) return postfixItems[0];
          }

          return null;
        }
        finally
        {
          var reparseRange = TextRange.FromLength(offset, reparseString.Length);
          document.DeleteText(reparseRange);
        }
      }

      private bool TemplateWithNameExists([NotNull] string prefix)
      {
        foreach (var providerInfo in myTemplatesManager.TemplateProvidersInfos)
        {
          if (providerInfo.Metadata.TemplateName == prefix) return true;
        }

        return false;
      }
    }
  }
}