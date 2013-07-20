using System;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion
{
  public static class PortabilityExtensions
  {
    public static PredefinedType GetPredefinedType([NotNull] this ITreeNode node)
    {
      return node.GetPsiModule().GetPredefinedType();
    }

    public static void CommitAllDocuments([NotNull] this IPsiServices services)
    {
      services.PsiManager.CommitAllDocuments();
    }

    public static void DoTransaction(
      [NotNull] this IPsiServices services, [NotNull] string commandName, [NotNull] Action action)
    {
      services.PsiManager.DoTransaction(action, commandName);
    }
  }
}

namespace JetBrains.Util
{
  public static class AssertionExtensions
  {
    [ContractAnnotation("value:null=>void;=>notnull")]
    public static T NotNull<T>(this T value) where T : class
    {
      if (value == null)
        Assertion.Fail("{0} is null", typeof(T).FullName);
      return value;
    }
  }
}