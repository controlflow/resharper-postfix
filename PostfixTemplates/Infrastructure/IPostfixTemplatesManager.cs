using System.Collections.Generic;
using JetBrains.Annotations;

namespace JetBrains.ReSharper.PostfixTemplates
{
  public interface IPostfixTemplatesManager
  {
    [NotNull, ItemNotNull] IEnumerable<IPostfixTemplateMetadata> AvailableTemplates { get; }
  }
}