using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi.Modules;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion
{
  public sealed class PostfixExecutionContext
  {
    public PostfixExecutionContext(
      [NotNull] IPsiModule psiModule,
      [NotNull] ILookupItemsOwner lookupItemsOwner, bool isForceMode,
      [CanBeNull] ReparsedCodeCompletionContext reparsedContext = null,
      [CanBeNull] string specificTemplateName = null)
    {
      PsiModule = psiModule;
      LookupItemsOwner = lookupItemsOwner;
      IsForceMode = isForceMode;
      ReparsedContext = reparsedContext;
      SpecificTemplateName = specificTemplateName;
    }

    [NotNull] public IPsiModule PsiModule { get; private set; }
    [NotNull] public ILookupItemsOwner LookupItemsOwner { get; private set; }
    public bool IsForceMode { get; private set; }

    [CanBeNull] public ReparsedCodeCompletionContext ReparsedContext { get; private set; }
    [CanBeNull] public string SpecificTemplateName { get; private set; }
  }
}