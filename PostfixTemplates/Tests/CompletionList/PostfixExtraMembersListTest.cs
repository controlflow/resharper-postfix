using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.PostfixTemplates.CompletionList
{
  [TestNetFramework4]
  public class PostfixExtraMembersListTest : PostfixCodeCompletionListTestBase
  {
    protected override string RelativeTestDataPath { get { return @"Extra\List"; } }

    [Test] public void TestStatic01() { DoNamedTest(); }
    [Test] public void TestStatic02() { DoNamedTest(); }
    [Test] public void TestStatic03() { DoNamedTest(); }

    [Test] public void TestEnum01() { DoNamedTest(); }
    [Test] public void TestEnum02() { DoNamedTest(); }
    [Test] public void TestEnum03() { DoNamedTest(); }
  }
}