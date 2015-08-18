using JetBrains.Annotations;
using JetBrains.ReSharper.PostfixTemplates.CodeCompletion;
using JetBrains.ReSharper.PostfixTemplates.Contexts;

namespace JetBrains.ReSharper.PostfixTemplates
{
  public interface IPostfixTemplate<in TPostfixTemplateContext>
    where TPostfixTemplateContext : PostfixTemplateContext
  {
    [CanBeNull] PostfixTemplateInfo TryCreateInfo([NotNull] TPostfixTemplateContext context);
    [NotNull] PostfixTemplateBehavior CreateBehavior([NotNull] PostfixTemplateInfo info);
  }
}