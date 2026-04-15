using System.Reflection;
using Flowthru.Core.Services;

namespace Flowthru.Tests.Examples.Infrastructure;

/// <summary>
/// Discovers Flowthru example projects by inspecting assemblies in the test output directory.
/// </summary>
/// <remarks>
/// <para>
/// Discovery strategy:
/// </para>
/// <list type="number">
///   <item>Enumerate <c>.dll</c> files in the test output directory
///         (populated at build time via <c>&lt;ProjectReference&gt;</c>).</item>
///   <item>Filter to assemblies where <see cref="Assembly.EntryPoint"/> is non-null —
///         this excludes library projects (<c>OutputType=Library</c>) without requiring
///         MSBuild metadata at runtime.</item>
///   <item>Verify the assembly contains a type with a <c>ConfigureServices</c> method
///         returning <see cref="IServiceProvider"/>.</item>
///   <item>Locate the source project by searching the <c>examples/</c> directory tree
///         for a matching <c>.csproj</c> file, excluding <c>examples/archived/</c>.</item>
/// </list>
/// </remarks>
public static class ExampleDiscovery
{
  private static readonly string WorkspaceRoot = FindWorkspaceRoot();
  private static readonly string ExamplesDirectory = Path.Combine(WorkspaceRoot, "examples");

  /// <summary>
  /// The build configuration (e.g. "Debug" or "Release") of the currently running test assembly,
  /// derived from its output path. Used to locate the matching example output directories.
  /// </summary>
  private static readonly string BuildConfiguration = InferBuildConfiguration();

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

    foreach (var dllPath in Directory.GetFiles(testOutputDir, "*.dll"))
    {
      var name = Path.GetFileNameWithoutExtension(dllPath);
      var assembly = TryLoadAssembly(name, testOutputDir);
      if (assembly == null)
      {
        continue;
      }

      // Libraries have a null EntryPoint; only executables (OutputType=Exe) are runnable examples.
      if (assembly.EntryPoint == null)
      {
        continue;
      }

      var entryPointType = FindConfigureServicesType(assembly);
      if (entryPointType == null)
      {
        continue;
      }

      var (sourcePath, category) = FindSourceProject(name);
      if (sourcePath == null)
      {
        continue;
      }

      yield return new ExampleProject
      {
        Name = name,
        ProjectPath = sourcePath,
        OutputPath = Path.Combine(
          WorkspaceRoot,
          "dist",
          "examples",
          category,
          name,
          BuildConfiguration,
          "net10.0"
        ),
        EntryPointType = entryPointType,
      };
    }
  }

  /// <summary>
  /// Searches the <c>examples/</c> directory tree for a <c>.csproj</c> matching
  /// <paramref name="assemblyName"/>, excluding <c>examples/archived/</c>.
  /// Returns the directory containing the <c>.csproj</c> and the top-level category folder
  /// (e.g. <c>"starter"</c> or <c>"advanced"</c>).
  /// </summary>
  private static (string? SourcePath, string Category) FindSourceProject(string assemblyName)
  {
    var matches = Directory.GetFiles(
      ExamplesDirectory,
      $"{assemblyName}.csproj",
      SearchOption.AllDirectories
    );

    foreach (var csproj in matches)
    {
      var relative = Path.GetRelativePath(ExamplesDirectory, csproj);
      var segments = relative.Split(Path.DirectorySeparatorChar);

      if (segments[0].Equals("archived", StringComparison.OrdinalIgnoreCase))
      {
        continue;
      }

      return (Path.GetDirectoryName(csproj)!, segments[0]);
    }

    return (null, string.Empty);
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
    {
      return loaded;
    }

    var dllPath = System.IO.Path.Combine(searchDirectory, $"{assemblyName}.dll");
    if (!File.Exists(dllPath))
    {
      return null;
    }

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
      {
        return dir;
      }

      dir = Directory.GetParent(dir)?.FullName;
    }

    throw new InvalidOperationException(
      "Could not find workspace root. Ensure nx.json exists in an ancestor directory."
    );
  }

  /// <summary>
  /// Infers the build configuration from the running test assembly's output path.
  /// The output path convention is <c>dist/…/&lt;Configuration&gt;/net10.0/</c>, so the
  /// segment immediately before the TFM folder is the configuration name.
  /// Falls back to <c>"Debug"</c> if the path doesn't match the expected shape.
  /// </summary>
  private static string InferBuildConfiguration()
  {
    var assemblyDir = Path.GetDirectoryName(typeof(ExampleDiscovery).Assembly.Location);
    if (assemblyDir == null)
    {
      return "Debug";
    }

    // assemblyDir ends with …/<Configuration>/net10.0 — parent is the configuration folder
    var configDir = Path.GetDirectoryName(assemblyDir);
    if (configDir == null)
    {
      return "Debug";
    }

    var config = Path.GetFileName(configDir);
    return string.IsNullOrEmpty(config) ? "Debug" : config;
  }
}
