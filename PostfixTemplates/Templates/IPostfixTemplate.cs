using JetBrains.Annotations;
using JetBrains.ReSharper.PostfixTemplates.Contexts;
using JetBrains.ReSharper.PostfixTemplates.LookupItems;

namespace JetBrains.ReSharper.PostfixTemplates.Templates
{
  public interface IPostfixTemplate<in TPostfixTemplateContext>
    where TPostfixTemplateContext : PostfixTemplateContext
  {
    [CanBeNull] PostfixTemplateInfo TryCreateInfo([NotNull] TPostfixTemplateContext context);
    [NotNull] PostfixTemplateBehavior CreateBehavior([NotNull] PostfixTemplateInfo info);
  }
}