using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Info;

namespace JetBrains.ReSharper.PostfixTemplates.CodeCompletion
{
  public class PostfixTemplateInfo : TextualInfo
  {
    public PostfixTemplateInfo([NotNull] string text, [NotNull] string identity, CodeCompletionContext context)
      : base(text, identity, context) { }
  }
}