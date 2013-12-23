using System.Windows.Forms.VisualStyles;
using JetBrains.Annotations;
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.LiveTemplates;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.PostfixTemplates
{
  public class PostfixExecutionContext
  {
    public PostfixExecutionContext(bool isForceMode, [NotNull] IPsiModule psiModule,
      [NotNull] ILookupItemsOwner lookupItemsOwner, [NotNull] string reparseString)
    {
      PsiModule = psiModule;
      LookupItemsOwner = lookupItemsOwner;
      ReparseString = reparseString;
      IsForceMode = isForceMode;

      LiveTemplatesManager = psiModule.GetSolution().GetComponent<LiveTemplatesManager>();
    }

    public bool IsForceMode { get; private set; }

    [NotNull] public IPsiModule PsiModule { get; private set; }
    [NotNull] public ILookupItemsOwner LookupItemsOwner { get; private set; }
    [NotNull] public string ReparseString { get; private set; }

    [NotNull] public LiveTemplatesManager LiveTemplatesManager { get; private set; }

    public virtual DocumentRange GetDocumentRange(ITreeNode treeNode)
    {
      return treeNode.GetDocumentRange();
    }

    [NotNull] public virtual PostfixExecutionContext WithForceMode(bool enabled)
    {
      if (enabled == IsForceMode) return this;

      return new PostfixExecutionContext(enabled, PsiModule, LookupItemsOwner, ReparseString);
    }
  }
}