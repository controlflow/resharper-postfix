using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion
{
  public static class PortabilityExtensions
  {
    public static PredefinedType GetPredefinedType([NotNull] this ITreeNode node)
    {
      return node.GetPsiModule().GetPredefinedType(node.GetResolveContext());
    }

    public static void CommitAllDocuments([NotNull] this IPsiServices services)
    {
      services.Files.CommitAllDocuments();
    }
  }
}