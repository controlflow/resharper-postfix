using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.ReSharper.PostfixTemplates;
using JetBrains.Threading;
using NUnit.Framework;

#pragma warning disable 618

#if RESHARPER8
using JetBrains.Util;
#elif RESHARPER9
using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.TestFramework;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.ReSharper.TestFramework;
using JetBrains.TestFramework.Application.Zones;
#endif

[assembly: TestDataPathBase(@".\Data\Completion")]

#if RESHARPER9
[ZoneDefinition]
public class IPostfixTestEnvironmentZone : ITestsZone, IRequire<PsiFeatureTestZone>
{ 
}
#endif

#if RESHARPER8
[SetUpFixture]
public class PostfixTestsAssembly : ReSharperTestEnvironmentAssembly
#else
[SetUpFixture]
public class ReSharperTestEnvironmentAssembly : TestEnvironmentAssembly<IPostfixTestEnvironmentZone>
#endif
{
  [NotNull]
  private static IEnumerable<Assembly> GetAssembliesToLoad()
  {
    yield return typeof(PostfixTemplatesManager).Assembly;
    yield return Assembly.GetExecutingAssembly();
  }

  public override void SetUp()
  {
    base.SetUp();
    ReentrancyGuard.Current.Execute("LoadAssemblies", () => {
      var assemblyManager = Shell.Instance.GetComponent<AssemblyManager>();
      assemblyManager.LoadAssemblies(GetType().Name, GetAssembliesToLoad());
    });
  }

  public override void TearDown()
  {
    ReentrancyGuard.Current.Execute("UnloadAssemblies", () => {
      var assemblyManager = Shell.Instance.GetComponent<AssemblyManager>();
      assemblyManager.UnloadAssemblies(GetType().Name, GetAssembliesToLoad());
    });

    base.TearDown();
  }
}