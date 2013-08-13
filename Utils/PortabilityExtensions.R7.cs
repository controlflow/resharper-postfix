using System;
using JetBrains.Annotations;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Hotspots;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

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

    public static TextRange GetHotspotRange(this DocumentRange documentRange)
    {
      return documentRange.TextRange;
    }

    public static bool IsForeachEnumeratorPatternType([NotNull] this ITypeElement typeElement)
    {
      var symbols = ResolveUtil
        .GetSymbolTableByTypeElement(typeElement, SymbolTableMode.FULL, typeElement.Module)
        .GetSymbolInfos("GetEnumerator");

      foreach (var symbol in symbols)
        if (CSharpDeclaredElementUtil.IsForeachEnumeratorPatternMember(symbol.GetDeclaredElement()))
          return true;

      return false;
    }

    public static void AdviceFinished(
      [NotNull] this HotspotSession session, [NotNull] Action<HotspotSession, TerminationType> action)
    {
      session.Finished += (hotspotSession, type) =>
      {
        action(hotspotSession, type);
      };
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