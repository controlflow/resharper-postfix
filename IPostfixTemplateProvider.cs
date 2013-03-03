using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.Lookup;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion
{
  public interface IPostfixTemplateProvider
  {
    void CreateItems([NotNull] PostfixTemplateAcceptanceContext context,
                     [NotNull] ICollection<ILookupItem> consumer);
  }
}