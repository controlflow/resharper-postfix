using JetBrains.Annotations;
using JetBrains.ReSharper.PostfixTemplates.Contexts;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.PostfixTemplates
{
  public interface IPostfixTemplateContextFactory
  {
    [CanBeNull] PostfixTemplateContext TryCreate([NotNull] ITreeNode position, [NotNull] PostfixTemplateExecutionContext executionContext);
  }
}