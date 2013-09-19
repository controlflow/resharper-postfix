using JetBrains.ReSharper.Feature.Services.Tests.CSharp.FeatureServices.CodeCompletion;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion
{
  [TestNetFramework4]
  public class PostfixCompletionStaticsListTest : CodeCompletionTestBase
  {
    protected override bool ExecuteAction { get { return false; } }
    protected override bool CheckAutomaticCompletionDefault() { return true; }

    protected override string RelativeTestDataPath
    {
      get { return PostfixCompletionTestsAssembly.TestDataPath + @"\Completion\Statics\List"; }
    }

    [Test] public void TestStatic01() { DoNamedTest(); }
    [Test] public void TestStatic02() { DoNamedTest(); }
    [Test] public void TestStatic03() { DoNamedTest(); }
  }
}