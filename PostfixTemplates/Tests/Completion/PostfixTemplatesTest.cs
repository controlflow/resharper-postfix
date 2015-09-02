using JetBrains.Application.Components;
using JetBrains.Application.Settings;
using JetBrains.Application.Settings.Store.Implementation;
using JetBrains.DataFlow;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.PostfixTemplates.Settings;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.PostfixTemplates.Completion
{
  [TestNetFramework4]
  public class PostfixTemplatesTest : PostfixCodeCompletionTestBase
  {
    protected override string RelativeTestDataPath { get { return string.Empty; } }

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
    [Test] public void TestIf19() { DoNamedTest(); }
    [Test] public void TestIf20() { DoNamedTest(); }
    [Test] public void TestIf21() { DoNamedTest(); }
    [Test] public void TestIf22() { DoNamedTest(); }

    [Test] public void TestNew01() { DoNamedTest(); }
    [Test] public void TestNew02() { DoNamedTest(); }
    [Test] public void TestNew03() { DoNamedTest(); }
    [Test] public void TestNew04() { DoNamedTest(); }
    [Test] public void TestNew05() { DoNamedTest(); }
    [Test] public void TestNew06() { DoNamedTest(); }
    [Test] public void TestNew07() { DoNamedTest(); }
    [Test] public void TestNew08() { DoNamedTest(); }

    [Test] public void TestVar01() { DoNamedTest(); }
    [Test] public void TestVar02() { DoNamedTest(); }
    [Test] public void TestVar03() { DoNamedTest(); }
    [Test] public void TestVar04() { DoNamedTest(); }
    [Test] public void TestVar05() { DoNamedTest(); }
    [Test] public void TestVar06() { DoNamedTest(); }
    [Test] public void TestVar07() { DoNamedTest(); }
    [Test] public void TestVar08() { DoNamedTest(); }
    [Test] public void TestVar09() { DoNamedTest(); }
    [Test] public void TestVar10() { DoNamedTest(); }
    [Test] public void TestVar11() { DoNamedTest(); }

    [Test] public void TestNot01() { DoNamedTest(); }
    [Test] public void TestNot02() { DoNamedTest(); }
    [Test] public void TestNot03() { DoNamedTest(); }

    [Test] public void TestNotNull01() { DoNamedTest(); }
    [Test] public void TestNotNull02() { DoNamedTest(); }

    [Test] public void TestField01() { DoNamedTest(); }
    [Test] public void TestField02() { DoNamedTest(); }

    [Test] public void TestProp01() { DoNamedTest(); }

    [Test] public void TestFor01() { DoNamedTest(); }
    [Test] public void TestFor02() { DoNamedTest(); }
    [Test] public void TestFor03() { DoNamedTest(); }
    [Test] public void TestFor04() { DoNamedTest(); }
    [Test] public void TestFor05() { DoNamedTest(); }

    [Test] public void TestForEach01() { DoNamedTest(); }
    [Test] public void TestForEach02() { DoNamedTest(); }
    [Test] public void TestForEach03() { DoNamedTest(); }
    [Test] public void TestForEach04() { DoNamedTest(); }
    [Test] public void TestForEach05() { DoNamedTest(); }

    [Test] public void TestReturn01() { DoNamedTest(); }
    [Test] public void TestReturn02() { DoNamedTest(); }
    [Test] public void TestReturn03() { DoNamedTest(); }
    [Test] public void TestReturn04() { DoNamedTest(); }
    [Test] public void TestReturn05() { DoNamedTest(); }
    [Test] public void TestReturn06() { DoNamedTest(); }

    [Test] public void TestThrow01() { DoNamedTest(); }
    [Test] public void TestThrow02() { DoNamedTest(); }
    [Test] public void TestThrow03() { DoNamedTest(); }
    [Test] public void TestThrow04() { DoNamedTest(); }
    [Test] public void TestThrow05() { DoNamedTest(); }
    [Test] public void TestThrow06() { DoNamedTest(); }
    [Test] public void TestThrow07() { DoNamedTest(); }
    [Test] public void TestThrow08() { DoNamedTest(); }

    [Test] public void TestLock01() { DoNamedTest(); }

    [Test] public void TestUsing01() { DoNamedTest(); }
    [Test] public void TestUsing02() { DoNamedTest(); }
    [Test] public void TestUsing03() { DoNamedTest(); }
    [Test] public void TestUsing04() { DoNamedTest(); }

    [Test] public void TestArg01() { DoNamedTest(); }
    [Test] public void TestArg02() { DoNamedTest(); }

    [Test] public void TestCast01() { DoNamedTest(); }
    [Test] public void TestCast02() { DoNamedTest(); }

    [Test] public void TestPar01() { DoNamedTest(); }

    [Test] public void TestTo01() { DoNamedTest(); }

    [Test] public void TestTypeof01() { DoNamedTest(); }
  }

  [TestNetFramework4]
  public class PostfixTemplatesBracelessTest : PostfixCodeCompletionTestBase
  {
    protected override string RelativeTestDataPath { get { return string.Empty; } }

    protected override void DoTest(IProject testProject)
    {
      Lifetimes.Using(lifetime =>
      {
        ChangeSettingsTemporarily(lifetime);

        var settingsStore = ShellInstance.GetComponent<SettingsStore>();
        var context = ContextRange.ManuallyRestrictWritesToOneContext((_, contexts) => contexts.Empty);
        var settings = settingsStore.BindToContextTransient(context);

        settings.SetValue((PostfixTemplatesSettings s) => s.UseBracesForEmbeddedStatements, false);

        base.DoTest(testProject);
      });
    }

    [Test] public void TestForEach40() { DoNamedTest(); }

    [Test] public void TestFor40() { DoNamedTest(); }
    [Test] public void TestFor41() { DoNamedTest(); }

    [Test] public void TestUsing40() { DoNamedTest(); }
  }
}