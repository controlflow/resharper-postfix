using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.LiveTemplates;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;

namespace JetBrains.ReSharper.PostfixTemplates.Contexts
{
  [PublicAPI]
  public class PostfixTemplateExecutionContext
  {
    public PostfixTemplateExecutionContext(
      [NotNull] ISolution solution, [NotNull] ITextControl textControl, [NotNull] IContextBoundSettingsStore settingsStore,
      [NotNull] string reparseString, bool isPreciseMode)
    {
      Solution = solution;
      TextControl = textControl;
      SettingsStore = settingsStore;
      ReparseString = reparseString;
      IsPreciseMode = isPreciseMode;
    }

    public bool IsPreciseMode { get; internal set; }

    [NotNull] public ISolution Solution { get; private set; }
    [NotNull] public ITextControl TextControl { get; private set; }

    [NotNull] public IContextBoundSettingsStore SettingsStore { get; private set; }

    [NotNull] public LiveTemplatesManager LiveTemplatesManager
    {
      get { return Solution.GetComponent<LiveTemplatesManager>(); }
    }

    [NotNull] public ILookupItemsOwner LookupItemsOwner
    {
      get
      {
        var factory = Solution.GetComponent<LookupItemsOwnerFactory>();
        return factory.CreateLookupItemsOwner(TextControl);
      }
    }

    [NotNull] public string ReparseString { get; private set; }

    public virtual DocumentRange GetDocumentRange(ITreeNode treeNode)
    {
      return treeNode.GetDocumentRange();
    }
  }
}