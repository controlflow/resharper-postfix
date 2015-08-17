using System;
using JetBrains.Annotations;
using JetBrains.ReSharper.PostfixTemplates.CodeCompletion;
using JetBrains.ReSharper.PostfixTemplates.Contexts;

namespace JetBrains.ReSharper.PostfixTemplates
{
  public interface IPostfixTemplate<in TPostfixTemplateContext>
    where TPostfixTemplateContext : PostfixTemplateContext
  {
    void PopulateTemplates([NotNull] TPostfixTemplateContext context, [NotNull] IPostfixTemplatesCollector collector);
  }

  public interface IPostfixTemplatesCollector
  {
    void Consume([NotNull] PostfixTemplateInfo info, [NotNull] Func<PostfixTemplateInfo, PostfixTemplateBehavior> behaviorFactory);
  }
}