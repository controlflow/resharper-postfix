using System.Collections.Generic;
using JetBrains.Annotations;

namespace JetBrains.ReSharper.PostfixTemplates
{
  // todo: who else can use this?
  // todo: what is the relation between this and IPostfixTemplateContextFactory

  public interface IPostfixTemplatesManager
  {
    [NotNull, ItemNotNull] IEnumerable<IPostfixTemplateMetadata> AvailableTemplates { get; }
  }
}