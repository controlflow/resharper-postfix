using JetBrains.ProjectModel;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.PostfixTemplates.Completion
{
  [TestNetFramework4]
  [TestFileExtension(RazorCSharpProjectFileType.RazorCSharpExtension)]
  public class PostfixRazorTest : PostfixCodeCompletionTestBase
  {
    protected override string RelativeTestDataPath { get { return @"Razor"; } }

    [Test] public void TestRazor01() { DoNamedTest("Model.cs"); }
    [Test] public void TestRazor02() { DoNamedTest("Model.cs"); }
    [Test] public void TestRazor03() { DoNamedTest("Model.cs"); }
    [Test] public void TestRazor04() { DoNamedTest("Model.cs"); }
    [Test] public void TestRazor05() { DoNamedTest("Model.cs"); }
    [Test] public void TestRazor06() { DoNamedTest("Model.cs"); }
    [Test] public void TestRazor07() { DoNamedTest("Model.cs"); }
    [Test] public void TestRazor08() { DoNamedTest("Model.cs"); }
    [Test] public void TestRazor09() { DoNamedTest("Model.cs"); }
    [Test] public void TestRazor10() { DoNamedTest("Model.cs"); }
  }
}