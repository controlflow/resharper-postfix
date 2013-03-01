using JetBrains.Application;
using JetBrains.Threading;
using System.Reflection;
using System.Collections.Generic;
using NUnit.Framework;

[SetUpFixture]
public class TestEnvironmentAssembly : ReSharperTestEnvironmentAssembly
{
  private static IEnumerable<Assembly> GetAssembliesToLoad()
  {
    yield return Assembly.GetExecutingAssembly();
    yield return typeof(object).Assembly;
  }

  public override void SetUp()
  {
    base.SetUp();

    ReentrancyGuard.Current.Execute(
      "LoadAssemblies",
      () => Shell.Instance
        .GetComponent<AssemblyManager>()
        .LoadAssemblies(GetType().Name, GetAssembliesToLoad()));
  }

  public override void TearDown()
  {
    ReentrancyGuard.Current.Execute(
      "UnloadAssemblies",
      () => Shell.Instance
        .GetComponent<AssemblyManager>()
        .UnloadAssemblies(GetType().Name, GetAssembliesToLoad()));

    base.TearDown();
  }
}