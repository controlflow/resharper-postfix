using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.PostfixTemplates.Contexts;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.PostfixTemplates
{
  // todo: who else can use this?
  // todo: what is the relation between this and IPostfixTemplateContextFactory

  public interface IPostfixTemplatesManager
  {
    [NotNull, ItemNotNull] IEnumerable<IPostfixTemplateMetadata> AvailableTemplates { get; }

    bool IsTemplateAvailableByName([NotNull] PostfixTemplateContext context, [NotNull] string templateName);
    void ExecuteTemplateByName([NotNull] PostfixTemplateContext context, [NotNull] string templateName, ITextControl textControl, TextRange nameRange);
  }
}