using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Feature.Services.Lookup;
#if RESHARPER7
using JetBrains.ReSharper.Psi;
#else
using JetBrains.ReSharper.Psi.Modules;
#endif

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion
{
  public sealed class PostfixExecutionContext
  {
    public PostfixExecutionContext(
      [NotNull] IPsiModule psiModule, [NotNull] ILookupItemsOwner lookupItemsOwner,
      [CanBeNull] ReparsedCodeCompletionContext reparsedContext = null,
      [NotNull] string specificTemplateName = null)
    {
      PsiModule = psiModule;
      LookupItemsOwner = lookupItemsOwner;
      ReparsedContext = reparsedContext;
      SpecificTemplateName = specificTemplateName;
    }

    [NotNull] public IPsiModule PsiModule { get; private set; }
    [NotNull] public ILookupItemsOwner LookupItemsOwner { get; private set; }

    [CanBeNull] public ReparsedCodeCompletionContext ReparsedContext { get; private set; }
    [CanBeNull] public string SpecificTemplateName { get; private set; }
  }
}