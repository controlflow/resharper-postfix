using JetBrains.ReSharper.FeaturesTestFramework.Completion;

namespace JetBrains.ReSharper.PostfixTemplates
{
  public abstract class PostfixCodeCompletionTestBase : CodeCompletionTestBase
  {
    protected override CodeCompletionTestType TestType
    {
      get { return CodeCompletionTestType.Action; }
    }

    protected override bool CheckAutomaticCompletionDefault() { return true; }
  }

  public abstract class PostfixCodeCompletionListTestBase : CodeCompletionTestBase
  {
    protected override CodeCompletionTestType TestType
    {
      get { return CodeCompletionTestType.List; }
    }

    protected override bool CheckAutomaticCompletionDefault() { return true; }
  }
}
