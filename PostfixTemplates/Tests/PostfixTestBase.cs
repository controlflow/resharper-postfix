#if RESHARPER8
using JetBrains.ReSharper.Feature.Services.Tests.CSharp.FeatureServices.CodeCompletion;
#elif RESHARPER9
using JetBrains.ReSharper.FeaturesTestFramework.Completion;
#endif

namespace JetBrains.ReSharper.PostfixTemplates
{
  public abstract class PostfixCodeCompletionTestBase : CodeCompletionTestBase
  {
#if RESHARPER8
    protected override bool ExecuteAction { get { return true; } }
#elif RESHARPER9
    protected override CodeCompletionTestType TestType { get { return CodeCompletionTestType.Action; } }
#endif

    protected override bool CheckAutomaticCompletionDefault() { return true; }
  }

  public abstract class PostfixCodeCompletionListTestBase : CodeCompletionTestBase
  {
#if RESHARPER8
    protected override bool ExecuteAction { get { return true; } }
#elif RESHARPER9
    protected override CodeCompletionTestType TestType { get { return CodeCompletionTestType.List; } }
#endif

    protected override bool CheckAutomaticCompletionDefault() { return true; }
  }
}
