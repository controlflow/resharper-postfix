using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.CodeCompletion;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.BaseInfrastructure;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Info;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.Psi;
using JetBrains.Util;
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
      if (wrapper != null) return wrapper.Info.AllDeclaredElements;

      return EmptyList<DeclaredElementInstance>.InstanceList;
    }

    [CanBeNull]
    public static DeclaredElementInstance GetDeclaredElement([NotNull] this ILookupItem lookupItem)
    {
      var wrapper = lookupItem as ILookupItemWrapper<DeclaredElementInfo>;
      if (wrapper == null) return null;

      return wrapper.Info.PreferredDeclaredElement;
    }

    public static void AddSomewhere([NotNull] this GroupedItemsCollector collector, [NotNull] ILookupItem lookupItem)
    {
      collector.AddToBottom(lookupItem);
    }

    public static bool IsAutoCompletion([NotNull] this CodeCompletionContext context)
    {
      return context.CodeCompletionType == CodeCompletionType.AutomaticCompletion;
    }

    public static bool IsAutoOrBasicCompletionType([NotNull] this CodeCompletionContext context)
    {
      return context.CodeCompletionType == CodeCompletionType.AutomaticCompletion
          || context.CodeCompletionType == CodeCompletionType.BasicCompletion;
    }
  }
}