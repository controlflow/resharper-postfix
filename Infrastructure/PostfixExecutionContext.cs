using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi.Modules;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion
{
  public sealed class PostfixExecutionContext
  {
    public PostfixExecutionContext(
      [NotNull] IPsiModule psiModule,
      [NotNull] ILookupItemsOwner lookupItemsOwner, bool isForceMode,
      [CanBeNull] string specificTemplateName = null)
    {
      PsiModule = psiModule;
      LookupItemsOwner = lookupItemsOwner;
      IsForceMode = isForceMode;
      SpecificTemplateName = specificTemplateName;
    }

    [NotNull] public IPsiModule PsiModule { get; private set; }
    [NotNull] public ILookupItemsOwner LookupItemsOwner { get; private set; }
    public bool IsForceMode { get; private set; }

    [CanBeNull] public string SpecificTemplateName { get; private set; }
  }
}