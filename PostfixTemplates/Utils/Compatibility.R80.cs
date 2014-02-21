using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.PostfixTemplates
{
  public static class Compatibility
  {
    [NotNull]
    public static IEnumerable<DeclaredElementInstance> GetAllDeclaredElements(
      [NotNull] this DeclaredElementLookupItem lookupItem)
    {
      foreach (var declaredElement in lookupItem.AllDeclaredElements)
      {
        yield return declaredElement;
      }
    }
  }
}
