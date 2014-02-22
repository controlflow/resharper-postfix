using JetBrains.Application.Components;
#if RESHARPER8
using JetBrains.ReSharper.Feature.Services.Tests.CSharp.FeatureServices.CodeCompletion;
#elif RESHARPER9
using JetBrains.ReSharper.FeaturesTestFramework.Completion;
#endif

namespace JetBrains.ReSharper.PostfixTemplates
{
  public abstract class PostfixCodeCompletionTestBase : CodeCompletionTestBase
  {
    protected override bool ExecuteAction { get { return true; } }
    protected override bool CheckAutomaticCompletionDefault() { return true; }
  }

  public abstract class PostfixCodeCompletionListTestBase : CodeCompletionTestBase
  {
    protected override bool ExecuteAction { get { return false; } }
    protected override bool CheckAutomaticCompletionDefault() { return true; }
  }
}
