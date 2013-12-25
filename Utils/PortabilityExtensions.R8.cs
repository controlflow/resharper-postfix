using System;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.PostfixTemplates
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

    public static void DoTransaction(
      [NotNull] this IPsiServices services, [NotNull] string commandName,
      [NotNull, InstantHandle] Action action)
    {
      services.Transactions.Execute(commandName, action);
    }

    public static T DoTransaction<T>(
      [NotNull] this IPsiServices services, [NotNull] string commandName,
      [NotNull, InstantHandle] Func<T> func)
    {
      var value = default(T);
      services.Transactions.Execute(commandName, () => value = func());
      return value;
    }

    public static bool IsForeachEnumeratorPatternType([NotNull] this ITypeElement typeElement)
    {
      return CSharpDeclaredElementUtil.IsForeachEnumeratorPatternType(typeElement);
    }
  }
}