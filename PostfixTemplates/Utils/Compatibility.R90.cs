using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.BaseInfrastructure;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Info;
using JetBrains.ReSharper.Features.Intellisense.CodeCompletion.CSharp.AspectLookupItems;
using JetBrains.ReSharper.Psi;
using ILookupItem = JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems.ILookupItem;

namespace JetBrains.ReSharper.PostfixTemplates
{
  public interface IPostfixLookupItem : ILookupItem { }

  public static class Compatibility
  {
    [NotNull]
    public static IEnumerable<DeclaredElementInstance> GetAllDeclaredElementInstances([NotNull] this ILookupItem lookupItem)
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
    public static DeclaredElementInstance GetDeclaredElement([NotNull] this ILookupItem lookupItem)
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