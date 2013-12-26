using JetBrains.Application.Settings;
using JetBrains.Application.Settings.Store.Implementation;
using JetBrains.DataFlow;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.DataContext;
using JetBrains.ReSharper.Feature.Services.Refactorings;
using JetBrains.ReSharper.Feature.Services.Tests.CSharp.FeatureServices.CodeCompletion;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.PostfixTemplates.Completion
{
  [TestNetFramework4]
  public class PostfixTemplatesTest : CodeCompletionTestBase
  {
    protected override bool ExecuteAction { get { return true; } }
    protected override bool CheckAutomaticCompletionDefault() { return true; }
    protected override string RelativeTestDataPath { get { return ""; } }

    protected override void DoTest(IProject testProject)
    {
      var settingsStore1 = Application.Shell.Instance.GetComponent<SettingsStore>();

      Lifetimes.Using(lifetime =>
      {
        ChangeSettingsTemporarily(lifetime);
        var settingsStore = settingsStore1.BindToContextTransient(ContextRange.Smart(testProject.ToDataContext()));
        settingsStore.SetValue((IntroduceVariableUseVarSettings s) => s.UseVarForIntroduceVariableRefactoringEvident, true);
        settingsStore.SetValue((IntroduceVariableUseVarSettings s) => s.UseVarForIntroduceVariableRefactoring, true);
        base.DoTest(testProject);
      });
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
    [Test] public void TestIf18() { DoNamedTest(); }

    [Test] public void TestNew01() { DoNamedTest(); }
    [Test] public void TestNew02() { DoNamedTest(); }
    [Test] public void TestNew03() { DoNamedTest(); }

    [Test] public void TestVar01() { DoNamedTest(); }
    [Test] public void TestVar02() { DoNamedTest(); }
    [Test] public void TestVar03() { DoNamedTest(); }
    [Test] public void TestVar04() { DoNamedTest(); }
    [Test] public void TestVar05() { DoNamedTest(); }
    [Test] public void TestVar06() { DoNamedTest(); }
    [Test] public void TestVar07() { DoNamedTest(); }
    [Test] public void TestVar08() { DoNamedTest(); }
    [Test] public void TestVar09() { DoNamedTest(); }

    [Test] public void TestNot01() { DoNamedTest(); }
    [Test] public void TestNot02() { DoNamedTest(); }

    [Test] public void TestNotNull01() { DoNamedTest(); }

    [Test] public void TestField01() { DoNamedTest(); }

    [Test] public void TestProp01() { DoNamedTest(); }

    [Test] public void TestFor01() { DoNamedTest(); }
    [Test] public void TestFor02() { DoNamedTest(); }
    [Test] public void TestFor03() { DoNamedTest(); }
    [Test] public void TestFor04() { DoNamedTest(); }
    [Test] public void TestFor05() { DoNamedTest(); }

    [Test] public void TestReturn01() { DoNamedTest(); }
    [Test] public void TestReturn02() { DoNamedTest(); }
    [Test] public void TestReturn03() { DoNamedTest(); }
    [Test] public void TestReturn04() { DoNamedTest(); }

    [Test] public void TestThrow01() { DoNamedTest(); }
    [Test] public void TestThrow02() { DoNamedTest(); }
    [Test] public void TestThrow03() { DoNamedTest(); }
    [Test] public void TestThrow04() { DoNamedTest(); }

    [Test] public void TestLock01() { DoNamedTest(); }

    [Test] public void TestEnum01() { DoNamedTest(); }
    [Test] public void TestEnum02() { DoNamedTest(); }
    [Test] public void TestEnum03() { DoNamedTest(); }

    [Test] public void TestPar01() { DoNamedTest(); }

    [Test] public void TestTypeof01() { DoNamedTest(); }
  }
}