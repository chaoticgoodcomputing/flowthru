using System.Reflection;
using Flowthru.Services;

namespace Flowthru.Tests.Examples.Infrastructure;

/// <summary>
/// Discovers Flowthru example projects by cross-referencing project-referenced assemblies
/// against the <c>examples/</c> directory structure.
/// </summary>
/// <remarks>
/// <para>
/// Discovery strategy (no manual <see cref="Assembly.LoadFrom"/>):
/// </para>
/// <list type="number">
///   <item>Enumerate project directories under <c>examples/starter/</c> and <c>examples/advanced/</c>.</item>
///   <item>For each directory whose name matches a <c>.csproj</c>, attempt to load the assembly
///         from the test output directory (where <c>&lt;ProjectReference&gt;</c> copies it at build time).</item>
///   <item>Verify the assembly contains a type with a <c>ConfigureServices</c> method
///         returning <see cref="IServiceProvider"/>.</item>
/// </list>
/// </remarks>
public static class ExampleDiscovery
{
  private static readonly string WorkspaceRoot = FindWorkspaceRoot();
  private static readonly string ExamplesDirectory = Path.Combine(WorkspaceRoot, "examples");

  /// <summary>
  /// Discovers all runnable example projects.
  /// </summary>
  public static IEnumerable<ExampleProject> DiscoverExamples()
  {
    if (!Directory.Exists(ExamplesDirectory))
    {
      throw new DirectoryNotFoundException(
        $"Examples directory not found at {ExamplesDirectory}. "
          + "Ensure the workspace root contains an 'examples/' directory."
      );
    }

    var testOutputDir = Path.GetDirectoryName(typeof(ExampleDiscovery).Assembly.Location)!;

    foreach (var (name, category, sourcePath) in GetExampleProjectDirectories())
    {
      var assembly = TryLoadAssembly(name, testOutputDir);
      if (assembly == null)
      {
        TestContext.Progress.WriteLine(
          $"[Discovery] Skipping {name}: assembly not found in {testOutputDir}"
        );
        continue;
      }

      var entryPoint = FindConfigureServicesType(assembly);
      if (entryPoint == null)
      {
        TestContext.Progress.WriteLine(
          $"[Discovery] Skipping {name}: no type with ConfigureServices(string?) found"
        );
        continue;
      }

      // Calculate output directory: dist/examples/{category}/{name}/net10.0/
      var outputPath = Path.Combine(WorkspaceRoot, "dist", "examples", category, name, "net10.0");

      yield return new ExampleProject
      {
        Name = name,
        ProjectPath = sourcePath,
        OutputPath = outputPath,
        EntryPointType = entryPoint,
      };
    }
  }

  /// <summary>
  /// Enumerates example project directories that contain a matching <c>.csproj</c> file.
  /// Searches <c>examples/starter/</c> and <c>examples/advanced/</c>.
  /// </summary>
  private static IEnumerable<(
    string Name,
    string Category,
    string SourcePath
  )> GetExampleProjectDirectories()
  {
    var categories = new[] { "starter", "advanced" };

    foreach (var category in categories)
    {
      var categoryDir = System.IO.Path.Combine(ExamplesDirectory, category);
      if (!Directory.Exists(categoryDir))
        continue;

      foreach (var projectDir in Directory.GetDirectories(categoryDir))
      {
        var name = System.IO.Path.GetFileName(projectDir);
        var csproj = System.IO.Path.Combine(projectDir, $"{name}.csproj");

        if (File.Exists(csproj))
          yield return (name, category, projectDir);
      }
    }
  }

  /// <summary>
  /// Loads an assembly by name from the test output directory.
  /// <c>&lt;ProjectReference&gt;</c> in the test csproj ensures example DLLs
  /// are copied here at build time — no manual path-walking required.
  /// </summary>
  private static Assembly? TryLoadAssembly(string assemblyName, string searchDirectory)
  {
    // Check already-loaded assemblies first (avoids redundant IO)
    var loaded = AppDomain
      .CurrentDomain.GetAssemblies()
      .FirstOrDefault(a => !a.IsDynamic && a.GetName().Name == assemblyName);

    if (loaded != null)
      return loaded;

    var dllPath = System.IO.Path.Combine(searchDirectory, $"{assemblyName}.dll");
    if (!File.Exists(dllPath))
      return null;

    try
    {
      return Assembly.LoadFrom(dllPath);
    }
    catch (Exception ex)
    {
      TestContext.Progress.WriteLine($"[Discovery] Failed to load {dllPath}: {ex.Message}");
      return null;
    }
  }

  /// <summary>
  /// Finds a type with a public static <c>ConfigureServices</c> method
  /// that returns <see cref="IServiceProvider"/>.
  /// </summary>
  private static Type? FindConfigureServicesType(Assembly assembly)
  {
    try
    {
      return assembly
        .GetTypes()
        .FirstOrDefault(t =>
          t.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Any(m => m.Name == "ConfigureServices" && m.ReturnType == typeof(IServiceProvider))
        );
    }
    catch (ReflectionTypeLoadException ex)
    {
      // Some types may fail to load if optional dependencies are missing.
      // Search the types that did load successfully.
      return ex
        .Types.Where(t => t != null)
        .FirstOrDefault(t =>
          t!
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Any(m => m.Name == "ConfigureServices" && m.ReturnType == typeof(IServiceProvider))
        );
    }
  }

  /// <summary>
  /// Walks up the directory tree from the current directory to find the workspace root,
  /// identified by the presence of <c>nx.json</c>.
  /// </summary>
  private static string FindWorkspaceRoot()
  {
    var dir = Directory.GetCurrentDirectory();

    while (dir != null)
    {
      if (File.Exists(System.IO.Path.Combine(dir, "nx.json")))
        return dir;

      dir = Directory.GetParent(dir)?.FullName;
    }

    throw new InvalidOperationException(
      "Could not find workspace root. Ensure nx.json exists in an ancestor directory."
    );
  }
}
