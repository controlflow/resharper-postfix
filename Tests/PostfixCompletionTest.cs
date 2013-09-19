using JetBrains.ReSharper.Feature.Services.Tests.CSharp.FeatureServices.CodeCompletion;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion
{
  [TestNetFramework4]
  public class PostfixCompletionTest : CodeCompletionTestBase
  {
    protected override bool ExecuteAction { get { return true; } }
    protected override bool CheckAutomaticCompletionDefault() { return true; }

    protected override string RelativeTestDataPath
    {
      // sad panda :(
      get { return PostfixCompletionTestsAssembly.TestDataPath + @"\Completion"; }
    }

    [Test] public void TestIf01() { DoNamedTest(); }
    [Test] public void TestIf02() { DoNamedTest(); }
    [Test] public void TestIf03() { DoNamedTest(); }
    [Test] public void TestIf04() { DoNamedTest(); }
    [Test] public void TestIf05() { DoNamedTest(); }
    [Test] public void TestIf06() { DoNamedTest(); }
    [Test] public void TestIf07() { DoNamedTest(); }
    [Test] public void TestIf08() { DoNamedTest(); }
    [Test] public void TestIf09() { DoNamedTest(); }
    [Test] public void TestIf10() { DoNamedTest(); }
    [Test] public void TestIf11() { DoNamedTest(); }
    [Test] public void TestIf12() { DoNamedTest(); }
    [Test] public void TestIf13() { DoNamedTest(); }
    [Test] public void TestIf14() { DoNamedTest(); }
    [Test] public void TestIf15() { DoNamedTest(); }
    [Test] public void TestIf16() { DoNamedTest(); }
    [Test] public void TestIf17() { DoNamedTest(); }

    [Test] public void TestNew01() { DoNamedTest(); }
    [Test] public void TestNew02() { DoNamedTest(); }
    [Test] public void TestNew03() { DoNamedTest(); }

    [Test] public void TestVar01() { DoNamedTest(); }
    [Test] public void TestVar02() { DoNamedTest(); }
    [Test] public void TestVar03() { DoNamedTest(); }
    [Test] public void TestVar04() { DoNamedTest(); }
    [Test] public void TestVar05() { DoNamedTest(); }
    [Test] public void TestVar06() { DoNamedTest(); }

    [Test] public void TestNot01() { DoNamedTest(); }
    [Test] public void TestNot02() { DoNamedTest(); }

    [Test] public void TestFor01() { DoNamedTest(); }
    [Test] public void TestFor02() { DoNamedTest(); }
    [Test] public void TestFor03() { DoNamedTest(); }
    [Test] public void TestFor04() { DoNamedTest(); }

    [Test] public void TestReturn01() { DoNamedTest(); }
    [Test] public void TestReturn02() { DoNamedTest(); }

    [Test] public void TestThrow01() { DoNamedTest(); }
    [Test] public void TestThrow02() { DoNamedTest(); }
    [Test] public void TestThrow03() { DoNamedTest(); }
    [Test] public void TestThrow04() { DoNamedTest(); }

    [Test] public void TestLock01() { DoNamedTest(); }

    [Test] public void TestEnum01() { DoNamedTest(); }
    [Test] public void TestEnum02() { DoNamedTest(); }
    [Test] public void TestEnum03() { DoNamedTest(); }
  }
}