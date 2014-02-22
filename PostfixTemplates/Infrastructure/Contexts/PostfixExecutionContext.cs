using JetBrains.Annotations;
using JetBrains.DataFlow;
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.LiveTemplates;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;

namespace JetBrains.ReSharper.PostfixTemplates
{
  [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
  public class PostfixExecutionContext
  {
    public PostfixExecutionContext([NotNull] Lifetime lifetime,
                                   [NotNull] ISolution solution,
                                   [NotNull] ITextControl textControl,
                                   [NotNull] ILookupItemsOwner lookupItemsOwner,
                                   [NotNull] string reparseString,
                                   bool isAutoCompletion)
    {
      Lifetime = lifetime;
      Solution = solution;
      TextControl = textControl;
      LookupItemsOwner = lookupItemsOwner;
      ReparseString = reparseString;
      IsAutoCompletion = isAutoCompletion;
      LiveTemplatesManager = solution.GetComponent<LiveTemplatesManager>();
    }

    public bool IsAutoCompletion { get; internal set; }

    [NotNull] public Lifetime Lifetime { get; private set; }
    [NotNull] public ISolution Solution { get; private set; }
    [NotNull] public ITextControl TextControl { get; private set; }
    [NotNull] public ILookupItemsOwner LookupItemsOwner { get; private set; }
    [NotNull] public string ReparseString { get; private set; }

    [NotNull] public LiveTemplatesManager LiveTemplatesManager { get; private set; }

    public virtual DocumentRange GetDocumentRange(ITreeNode treeNode)
    {
      return treeNode.GetDocumentRange();
    }
  }
}