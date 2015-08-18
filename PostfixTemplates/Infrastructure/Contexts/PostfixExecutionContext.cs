using JetBrains.Annotations;
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.LiveTemplates;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;

namespace JetBrains.ReSharper.PostfixTemplates.Contexts
{
  // todo: IContextBoundSettingsStore here

  [PublicAPI]
  public class PostfixExecutionContext
  {
    [CanBeNull] private ILookupItemsOwner myLookupItemsOwner;
    [CanBeNull] private LiveTemplatesManager myLiveTemplatesManager;

    public PostfixExecutionContext(
      [NotNull] ISolution solution, [NotNull] ITextControl textControl, [NotNull] string reparseString, bool isPreciseMode)
    {
      Solution = solution;
      TextControl = textControl;
      ReparseString = reparseString;
      IsPreciseMode = isPreciseMode;
      myLiveTemplatesManager = solution.GetComponent<LiveTemplatesManager>();
    }

    public bool IsPreciseMode { get; internal set; }

    [NotNull] public ISolution Solution { get; private set; }
    [NotNull] public ITextControl TextControl { get; private set; }

    [NotNull]
    public ILookupItemsOwner LookupItemsOwner
    {
      get
      {
        if (myLookupItemsOwner != null) return myLookupItemsOwner;

        var factory = Solution.GetComponent<LookupItemsOwnerFactory>();
        return (myLookupItemsOwner = factory.CreateLookupItemsOwner(TextControl));
      }
    }

    [NotNull] public string ReparseString { get; private set; }

    [NotNull] public LiveTemplatesManager LiveTemplatesManager
    {
      get
      {
        if (myLiveTemplatesManager != null) return myLiveTemplatesManager;

        return (myLiveTemplatesManager = Solution.GetComponent<LiveTemplatesManager>());
      }
    }

    public virtual DocumentRange GetDocumentRange(ITreeNode treeNode)
    {
      return treeNode.GetDocumentRange();
    }
  }
}