using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.PostfixTemplates.Contexts
{
  public interface IPostfixTemplateContextFactory
  {
    [CanBeNull] PostfixTemplateContext TryCreate([NotNull] ITreeNode position, [NotNull] PostfixTemplateExecutionContext executionContext);
  }
}