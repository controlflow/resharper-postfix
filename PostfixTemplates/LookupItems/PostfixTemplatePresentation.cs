using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Presentations;
using JetBrains.ReSharper.Feature.Services.Resources;

namespace JetBrains.ReSharper.PostfixTemplates.CodeCompletion
{
  public class PostfixTemplatePresentation : TextPresentation<PostfixTemplateInfo>
  {
    public PostfixTemplatePresentation([NotNull] PostfixTemplateInfo info)
      : base(info, ServicesThemedIcons.LiveTemplate.Id, false)
    {
    }
  }
}