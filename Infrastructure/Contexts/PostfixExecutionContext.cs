using JetBrains.Annotations;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion
{
  public class PostfixExecutionContext
  {
    public PostfixExecutionContext(
      bool isForceMode, [NotNull] IPsiModule psiModule,
      [NotNull] ILookupItemsOwner lookupItemsOwner,
      [NotNull] string reparseString)
    {
      PsiModule = psiModule;
      LookupItemsOwner = lookupItemsOwner;
      ReparseString = reparseString;
      IsForceMode = isForceMode;
    }

    public bool IsForceMode { get; private set; }

    [NotNull] public IPsiModule PsiModule { get; private set; }
    [NotNull] public ILookupItemsOwner LookupItemsOwner { get; private set; }
    [NotNull] public string ReparseString { get; private set; }

    public virtual DocumentRange GetDocumentRange(ITreeNode treeNode)
    {
      return treeNode.GetDocumentRange();
    }
  }
}