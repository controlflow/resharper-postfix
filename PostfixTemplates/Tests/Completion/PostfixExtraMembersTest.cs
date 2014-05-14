using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.PostfixTemplates.Completion
{
  [TestNetFramework4]
  public class PostfixExtraMembersTest : PostfixCodeCompletionTestBase
  {
    protected override string RelativeTestDataPath { get { return @"Extra"; } }

    [Test] public void TestStatic01() { DoNamedTest(); }
    [Test] public void TestStatic02() { DoNamedTest(); }
    [Test] public void TestStatic03() { DoNamedTest(); }
    [Test] public void TestStatic04() { DoNamedTest(); }
    [Test] public void TestStatic05() { DoNamedTest(); }
    [Test] public void TestStatic06() { DoNamedTest(); }
    [Test] public void TestStatic07() { DoNamedTest(); }
    [Test] public void TestStatic08() { DoNamedTest(); }
    [Test] public void TestStatic09() { DoNamedTest(); }
    [Test] public void TestStatic10() { DoNamedTest(); }
    [Test] public void TestStatic11() { DoNamedTest(); }
    [Test] public void TestStatic12() { DoNamedTest(); }
    [Test] public void TestStatic13() { DoNamedTest(); }

    [Test] public void TestEnum01() { DoNamedTest(); }
    [Test] public void TestEnum02() { DoNamedTest(); }
    [Test] public void TestEnum03() { DoNamedTest(); }
    [Test] public void TestEnum04() { DoNamedTest(); }

    [Test] public void TestCount01() { DoNamedTest(); }
    [Test] public void TestCount02() { DoNamedTest(); }
    [Test] public void TestCount03() { DoNamedTest(); }
    [Test] public void TestCount04() { DoNamedTest(); }
    [Test] public void TestLength01() { DoNamedTest(); }
    [Test] public void TestLength02() { DoNamedTest(); }
  }
}