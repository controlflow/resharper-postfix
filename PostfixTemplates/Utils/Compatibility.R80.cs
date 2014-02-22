using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi;
using JetBrains.Util;

namespace JetBrains.ReSharper.PostfixTemplates
{
  public static class Compatibility
  {
    [NotNull]
    public static IEnumerable<DeclaredElementInstance> GetAllDeclaredElementInstances([NotNull] this ILookupItem lookupItem)
    {
      var declaredElementItem = lookupItem as DeclaredElementLookupItem;
      if (declaredElementItem != null)
      {
        return declaredElementItem.AllDeclaredElements;
      }

      return EmptyList<DeclaredElementInstance>.InstanceList;
    }

    [CanBeNull]
    public static DeclaredElementInstance GetDeclaredElement([NotNull] this ILookupItem lookupItem)
    {
      var declaredElementItem = lookupItem as DeclaredElementLookupItem;
      if (declaredElementItem == null) return null;

      return declaredElementItem.PreferredDeclaredElement;
    }
  }
}
