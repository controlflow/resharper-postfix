using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.PostfixTemplates.Contexts;

namespace JetBrains.ReSharper.PostfixTemplates
{
  public interface IPostfixTemplate
  {
    [CanBeNull] ILookupItem CreateItem([NotNull] PostfixTemplateContext context);
  }
}