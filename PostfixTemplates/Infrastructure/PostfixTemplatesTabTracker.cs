using System.Linq;
using JetBrains.ActionManagement;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.Application.CommandProcessing;
using JetBrains.Application.DataContext;
using JetBrains.DataFlow;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.LiveTemplates;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Feature.Services.Util;
using JetBrains.ReSharper.PostfixTemplates.Contexts;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;
using JetBrains.TextControl.Util;
using JetBrains.UI.ActionsRevised;
using JetBrains.UI.ActionSystem.Text;
using JetBrains.Util;

namespace JetBrains.ReSharper.PostfixTemplates
{
  // todo: no lookup items here!
  // todo: [R#] press Tab to expand postfix template hint
  // todo: [R#] availability check without commit like in live templates
  // todo: [R#] merge with live templates availability check?

  [ShellComponent]
  public sealed class PostfixTemplatesTabTracker
  {
    public PostfixTemplatesTabTracker(
      [NotNull] Lifetime lifetime,
      [NotNull] IActionManager manager,
      [NotNull] ICommandProcessor commandProcessor,
      [NotNull] TextControlChangeUnitFactory changeUnitFactory)
    {
      // override live templates expand action
      var expandAction = manager.Defs.TryGetActionDefById(TextControlActions.TAB_ACTION_ID);
      if (expandAction != null)
      {
        var postfixHandler = new ExpandPostfixTemplateHandler(commandProcessor, changeUnitFactory);

        lifetime.AddBracket(
          FOpening: () => manager.Handlers.AddHandler(expandAction, postfixHandler),
          FClosing: () => manager.Handlers.RemoveHandler(expandAction, postfixHandler));
      }
    }

    private sealed class ExpandPostfixTemplateHandler : IExecutableAction
    {
      [NotNull] private readonly ICommandProcessor myCommandProcessor;
      [NotNull] private readonly TextControlChangeUnitFactory myChangeUnitFactory;

      public ExpandPostfixTemplateHandler(
        [NotNull] ICommandProcessor commandProcessor, [NotNull] TextControlChangeUnitFactory changeUnitFactory)
      {
        myCommandProcessor = commandProcessor;
        myChangeUnitFactory = changeUnitFactory;
      }

      /*
      IActionRequirement IActionWithExecuteRequirement.GetRequirement(IDataContext dataContext)
      {
        if (!FastCheckAvailable(dataContext))
          return EmptyRequirement.Instance;

        return CurrentPsiFileRequirement.FromDataContext(dataContext);
      }

      private bool FastCheckAvailable(IDataContext dataContext)
      {
        var solution = dataContext.GetData(ProjectModel.DataContext.DataConstants.SOLUTION);
        if (solution == null) return false;

        var textControl = dataContext.GetData(TextControl.DataContext.DataConstants.TEXT_CONTROL);
        if (textControl == null) return false;

        return TODO;
      }
      */

      public bool Update(IDataContext dataContext, ActionPresentation presentation, DelegateUpdate nextUpdate)
      {
        var solution = dataContext.GetData(ProjectModel.DataContext.DataConstants.SOLUTION);
        if (solution == null) return nextUpdate();

        var textControl = dataContext.GetData(TextControl.DataContext.DataConstants.TEXT_CONTROL);
        if (textControl == null) return nextUpdate();

        return IsAvailableOrExecuteEww(solution, textControl, execute: false) || nextUpdate();
      }

      public void Execute(IDataContext context, DelegateExecute nextExecute)
      {
        var solution = context.GetData(ProjectModel.DataContext.DataConstants.SOLUTION);
        if (solution == null) return;

        var textControl = context.GetData(TextControl.DataContext.DataConstants.TEXT_CONTROL);
        if (textControl == null) return;

        var lookupWindowManager = solution.TryGetComponent<ILookupWindowManager>();
        if (lookupWindowManager != null && lookupWindowManager.CurrentLookup != null) return;

        const string commandName = "Expanding postfix template with [Tab]";

        var updateCookie = myChangeUnitFactory.CreateChangeUnit(textControl, commandName);

        try
        {
          using (myCommandProcessor.UsingCommand(commandName))
          {
            IsAvailableOrExecuteEww(solution, textControl, execute: true);
          }
        }
        catch
        {
          updateCookie.Dispose();
          throw;
        }

        nextExecute();
      }

      private bool IsAvailableOrExecuteEww([NotNull] ISolution solution, [NotNull] ITextControl textControl, bool execute)
      {
        var offset = textControl.Caret.Offset();
        var prefix = LiveTemplatesManager.GetPrefix(textControl.Document, offset);

        if (!TemplateWithNameExists(prefix)) return false;

        var files = textControl.Document.GetPsiSourceFiles(solution);
        var allLanguages = files.SelectMany(file => file.GetPsiServices().Files.GetLanguages(file, PsiLanguageCategories.Primary)).Distinct();

        foreach (var psiLanguageType in allLanguages)
        {
          var contextFactory = LanguageManager.Instance.TryGetService<IPostfixTemplateContextFactory>(psiLanguageType);
          if (contextFactory == null) continue;

          foreach (var reparseString in contextFactory.GetReparseStrings())
          {
            var templateContext = TryReparseWith(solution, textControl, reparseString);
            if (templateContext != null)
            {
              var templatesManager = LanguageManager.Instance.GetService<IPostfixTemplatesManager>(psiLanguageType);

              if (execute)
              {
                var nameRange = TextRange.FromLength(offset - prefix.Length, prefix.Length);
                templatesManager.ExecuteTemplateByName(templateContext, prefix, textControl, nameRange);
                return true;
              }

              return templatesManager.IsTemplateAvailableByName(templateContext, prefix);
            }
          }
        }

        return false;
      }

      [CanBeNull]
      private PostfixTemplateContext TryReparseWith([NotNull] ISolution solution, [NotNull] ITextControl textControl, [NotNull] string reparseString)
      {
        var offset = textControl.Caret.Offset();
        var document = textControl.Document;

        try
        {
          document.InsertText(offset, reparseString);

          solution.GetPsiServices().Files.CommitAllDocuments();

          foreach (var position in TextControlToPsi.GetElements<ITreeNode>(solution, document, offset))
          {
            var templateContextFactory = LanguageManager.Instance.TryGetService<IPostfixTemplateContextFactory>(position.Language);
            if (templateContextFactory != null)
            {
              var executionContext = new PostfixTemplateExecutionContext(
                solution, textControl, position.GetSettingsStore(), reparseString, false);

              return templateContextFactory.TryCreate(position, executionContext);
            }
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
        foreach (var manager in LanguageManager.Instance.GetServicesFromAll<IPostfixTemplatesManager>())
        {
          foreach (var providerInfo in manager.AvailableTemplates)
          {
            if (providerInfo.Metadata.TemplateName == prefix) return true;
          }
        }

        return false;
      }
    }
  }
}