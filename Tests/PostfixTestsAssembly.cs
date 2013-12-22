using System;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.DataFlow;
using JetBrains.ReSharper.PostfixTemplates;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.JavaScript.Impl.PsiModules.ReferencedFilesSupport;
using JetBrains.Threading;
using JetBrains.Util;
using NUnit.Framework;

// ReSharper disable once CheckNamespace
[SetUpFixture]
public class PostfixTestsAssembly : ReSharperTestEnvironmentAssembly
{
  static PostfixTestsAssembly()
  {
    var binPath = FileSystemPath.Parse(Environment.CurrentDirectory);
    var parentDirectory = binPath.Directory.Directory;
    TestDataPath = parentDirectory.Combine("Data").FullPath;
  }

  [NotNull] public readonly static string TestDataPath;

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

[PsiComponent]
internal class JavaScriptDependentFilesCacheHack : JavaScriptDependentFilesCache
{
  public JavaScriptDependentFilesCacheHack(
    Lifetime lifetime, IViewable<ILibraryFiles> libraryFiles,
    JavaScriptDependentFilesModuleFactory dependentFilesModuleFactory,
    JavaScriptDependentFilesBuilder builder, IShellLocks locks,
    IPsiConfiguration configuration, IPersistentIndexManager persistentIndexManager)
    : base(lifetime, new ListEvents<ILibraryFiles>(lifetime, "booo"),
           dependentFilesModuleFactory, builder,
           locks, configuration, persistentIndexManager) { }
}