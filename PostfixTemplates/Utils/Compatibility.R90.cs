using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.BaseInfrastructure;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Info;
using JetBrains.ReSharper.Psi;

// ReSharper disable once CheckNamespace
namespace JetBrains.ReSharper.Feature.Services.Lookup
{
  public interface ILookupItem : CodeCompletion.Infrastructure.LookupItems.ILookupItem { }

  public static class Compatibility
  {
    [NotNull]
    public static IEnumerable<DeclaredElementInstance> GetAllDeclaredElementInstances(
      [NotNull] this CodeCompletion.Infrastructure.LookupItems.ILookupItem lookupItem)
    {
      var wrapper = lookupItem as ILookupItemWrapper<DeclaredElementInfo>;
      if (wrapper != null)
      {
        foreach (var instance in wrapper.Info.AllDeclaredElements)
        {
          yield return instance;
        }
      }
    }

    [CanBeNull]
    public static DeclaredElementInstance GetDeclaredElement(
      [NotNull] this CodeCompletion.Infrastructure.LookupItems.ILookupItem lookupItem)
    {
      var wrapper = lookupItem as ILookupItemWrapper<DeclaredElementInfo>;
      if (wrapper != null)
      {
        return wrapper.Info.PreferredDeclaredElement;
      }

      return null;
    }
  }
}