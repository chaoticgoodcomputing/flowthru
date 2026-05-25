using System.Reflection;
using Flowthru.Hosting;

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
        OutputPath = ResolveExampleOutputPath(category, name),
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
  /// Walks up the directory tree to find the workspace root, identified by the presence
  /// of <c>nx.json</c>. Tries the current directory first (default for Nx-driven runs from
  /// the workspace root), then falls back to the test assembly's directory — necessary when
  /// the test host runs against a per-shard publish output where vstest sets cwd to the
  /// DLL's directory rather than the workspace.
  /// </summary>
  private static string FindWorkspaceRoot()
  {
    foreach (
      var start in new[]
      {
        Directory.GetCurrentDirectory(),
        Path.GetDirectoryName(typeof(ExampleDiscovery).Assembly.Location),
      }
    )
    {
      var dir = start;
      while (dir != null)
      {
        if (File.Exists(Path.Combine(dir, "nx.json")))
        {
          return dir;
        }

        dir = Directory.GetParent(dir)?.FullName;
      }
    }

    throw new InvalidOperationException(
      "Could not find workspace root. Ensure nx.json exists in an ancestor directory of "
        + "either the cwd or the test assembly location."
    );
  }

  /// <summary>
  /// Probe the filesystem for the example's actual build output directory
  /// rather than inferring a configuration name from the test assembly's
  /// path. The test runner executes from a sharded copy
  /// (<c>dist/tests/integration/Flowthru.Tests.Examples/shards/&lt;Example&gt;/</c>)
  /// whose path segments are unrelated to the example's MSBuild
  /// <c>$(Configuration)</c>, so any inference scheme rooted at the
  /// test assembly's location is structurally wrong.
  /// </summary>
  /// <remarks>
  /// Tries <c>Debug</c> first because that's the dominant nx-driven
  /// path, then <c>Release</c>, then the flat <c>net10.0</c> layout
  /// some nx configurations produce. Returns the Debug path as a
  /// last resort so downstream consumers fail loudly rather than
  /// silently fall back to <c>AppContext.BaseDirectory</c>, which is
  /// the shard — exactly what we want to avoid.
  /// </remarks>
  private static string ResolveExampleOutputPath(string category, string name)
  {
    foreach (var config in new[] { "Debug", "Release" })
    {
      var candidate = Path.Combine(
        WorkspaceRoot, "dist", "examples", category, name, config, "net10.0"
      );
      if (Directory.Exists(candidate)) return candidate;
    }

    var flat = Path.Combine(
      WorkspaceRoot, "dist", "examples", category, name, "net10.0"
    );
    if (Directory.Exists(flat)) return flat;

    // No output dir found — return the Debug candidate so the failure
    // path is explicit (FileNotFoundException at first use) instead
    // of silently resolving to a wrong venv.
    return Path.Combine(
      WorkspaceRoot, "dist", "examples", category, name, "Debug", "net10.0"
    );
  }
}
