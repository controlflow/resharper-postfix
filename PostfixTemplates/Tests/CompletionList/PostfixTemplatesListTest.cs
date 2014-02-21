using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.PostfixTemplates.CompletionList
{
  [TestNetFramework4]
  public class PostfixTemplatesListTest : PostfixCodeCompletionListTestBase
  {
    protected override string RelativeTestDataPath { get { return @"List"; } }

    [Test] public void TestIf01() { DoNamedTest(); }

    [Test] public void TestNamespace01() { DoNamedTest(); }

    [Test] public void TestUnresolved01() { DoNamedTest(); }
    [Test] public void TestUnresolved02() { DoNamedTest(); }
    [Test] public void TestUnresolved03() { DoNamedTest(); }
    [Test] public void TestUnresolved04() { DoNamedTest(); }
    [Test] public void TestUnresolved05() { DoNamedTest(); }

    [Test] public void TestType01() { DoNamedTest(); }
    [Test] public void TestType02() { DoNamedTest(); }
    [Test] public void TestType03() { DoNamedTest(); }
    [Test] public void TestType04() { DoNamedTest(); }
    [Test] public void TestType05() { DoNamedTest(); }

    [Test] public void TestVar01() { DoNamedTest(); }
    [Test] public void TestVar02() { DoNamedTest(); }

    [Test] public void TestNew01() { DoNamedTest(); }
    [Test] public void TestNew02() { DoNamedTest(); }

    [Test] public void TestSwitch01() { DoNamedTest(); }

    [Test] public void TestThis01() { DoNamedTest(); }

    [Test] public void TestBase01() { DoNamedTest(); }

    [Test] public void TestBoolean01() { DoNamedTest(); }
    [Test] public void TestBoolean02() { DoNamedTest(); }
    [Test] public void TestBoolean03() { DoNamedTest(); }
  }
}