using JetBrains.ReSharper.Feature.Services.Tests.CSharp.FeatureServices.CodeCompletion;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.Tests
{
  [TestFixture, TestNetFramework4]
  public sealed class TemplateProvidersCompletionListTest : CodeCompletionTestBase
  {
    protected override bool ExecuteAction { get { return false; } }

    [Test] void Test01() { DoNamedTest2(); }
  }
}