using System;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion;
using JetBrains.Threading;
using JetBrains.Util;
using NUnit.Framework;

// ReSharper disable once CheckNamespace
[SetUpFixture]
public class ReSharperPostfixCompletionTestsAssembly : ReSharperTestEnvironmentAssembly
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

    SetupBaseTestPath();
  }

  private static void SetupBaseTestPath()
  {
    var binPath = FileSystemPath.Parse(Environment.CurrentDirectory);
    var parentDirectory = binPath.Directory.Directory;
    var testDataDirectory = parentDirectory.Combine("Data");

    Environment.SetEnvironmentVariable("BASE_TEST_DATA", testDataDirectory.FullPath);
    Environment.SetEnvironmentVariable("TEST_DATA", testDataDirectory.FullPath);
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