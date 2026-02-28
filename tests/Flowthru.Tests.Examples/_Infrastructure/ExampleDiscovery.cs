using System.Reflection;

namespace Flowthru.Tests.Examples.Infrastructure;

/// <summary>
/// Discovers Flowthru example projects from the examples directory.
/// Auto-discovers all examples from starter/ and advanced/ subdirectories.
/// </summary>
public static class ExampleDiscovery
{
  private static readonly string _workspaceRoot = GetWorkspaceRoot();
  private static readonly string _examplesDirectory = Path.Combine(_workspaceRoot, "examples");

  // Static constructor to ensure example assemblies are loaded
  static ExampleDiscovery()
  {
    LoadExampleAssemblies();
  }

  /// <summary>
  /// Discovers all executable example projects in the examples directory.
  /// Scans both examples/starter/ and examples/advanced/ subdirectories.
  /// </summary>
  /// <returns>A collection of discovered example projects.</returns>
  public static IEnumerable<ExampleProject> DiscoverExamples()
  {
    if (!Directory.Exists(_examplesDirectory))
    {
      throw new DirectoryNotFoundException($"Examples directory not found: {_examplesDirectory}");
    }

    // Get all loaded assemblies that match example project patterns
    var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
    var exampleAssemblies = loadedAssemblies
      .Where(a => !a.IsDynamic && a.GetName().Name != null)
      .Where(a => IsExampleAssembly(a))
      .OrderBy(a => a.GetName().Name)
      .ToList();

    foreach (var assembly in exampleAssemblies)
    {
      var projectName = assembly.GetName().Name!;
      var projectDir = FindProjectDirectory(projectName);

      if (projectDir == null)
      {
        Console.WriteLine($"Warning: Could not find project directory for {projectName}");
        continue;
      }

      var entryPointType = FindEntryPointType(assembly, projectName);
      if (entryPointType == null)
      {
        Console.WriteLine($"Warning: Could not find Program entry point for {projectName}");
        continue;
      }

      // Check if the example has required data files (for examples that need external datasets)
      if (!HasRequiredDataFiles(projectDir, projectName))
      {
        Console.WriteLine($"Skipping {projectName}: required data files not found");
        continue;
      }

      yield return new ExampleProject
      {
        Name = projectName,
        ProjectPath = projectDir,
        CsprojPath = Path.Combine(projectDir, $"{projectName}.csproj"),
        EntryPointType = entryPointType,
      };
    }
  }

  /// <summary>
  /// Determines if an assembly is an example project from starter/ or advanced/ directories.
  /// </summary>
  private static bool IsExampleAssembly(Assembly assembly)
  {
    var projectName = assembly.GetName().Name;
    if (string.IsNullOrEmpty(projectName))
    {
      return false;
    }

    // Check if a matching project directory exists in examples/starter/ or examples/advanced/
    var starterPath = Path.Combine(_examplesDirectory, "starter", projectName);
    var advancedPath = Path.Combine(_examplesDirectory, "advanced", projectName);

    return Directory.Exists(starterPath) || Directory.Exists(advancedPath);
  }

  /// <summary>
  /// Loads example assemblies from the output directory to ensure they are available for discovery.
  /// </summary>
  private static void LoadExampleAssemblies()
  {
    try
    {
      // Find the test assembly output directory
      var testAssemblyPath = typeof(ExampleDiscovery).Assembly.Location;
      var testOutputDir = Path.GetDirectoryName(testAssemblyPath);

      if (string.IsNullOrEmpty(testOutputDir))
      {
        return;
      }

      // Navigate to the repo dist directory where examples are built
      // Path structure: dist/tests/Flowthru.Tests.Examples/net10.0/ -> dist/examples/
      var distDir = Path.GetFullPath(Path.Combine(testOutputDir, "..", "..", ".."));
      var examplesOutputDir = Path.Combine(distDir, "examples");

      if (!Directory.Exists(examplesOutputDir))
      {
        return;
      }

      // Scan for example assemblies in starter/ and advanced/
      var exampleDlls = Directory
        .GetFiles(examplesOutputDir, "*.dll", SearchOption.AllDirectories)
        .Where(f =>
        {
          var fileName = Path.GetFileNameWithoutExtension(f);
          // Filter to main assemblies (not deps, resources, etc.)
          return !fileName.Contains(".resources")
            && !fileName.StartsWith("System.")
            && !fileName.StartsWith("Microsoft.");
        })
        .ToList();

      foreach (var dllPath in exampleDlls)
      {
        try
        {
          var assemblyName = AssemblyName.GetAssemblyName(dllPath);
          // Check if already loaded
          var existingAssembly = AppDomain
            .CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.FullName == assemblyName.FullName);

          if (existingAssembly == null)
          {
            Assembly.LoadFrom(dllPath);
          }
        }
        catch
        {
          // Skip assemblies that fail to load
        }
      }
    }
    catch
    {
      // Best effort - if loading fails, discovery will work with whatever is already loaded
    }
  }

  /// <summary>
  /// Checks if an example project has its required data files.
  /// Some examples (like UmapReferenceComparisons) require external datasets to be generated/downloaded.
  /// </summary>
  private static bool HasRequiredDataFiles(string projectDir, string projectName)
  {
    // UmapReferenceComparisons requires input datasets that must be generated via Python scripts
    // Since it tries to run all pipelines, ALL datasets must be present
    if (projectName.Equals("UmapReferenceComparisons", StringComparison.OrdinalIgnoreCase))
    {
      var inputDir = Path.Combine(projectDir, "Data", "_01_Raw", "Datasets", "Inputs");

      // All expected datasets must exist since ExecuteAllPipelines will try to run them
      var expectedDatasets = new[] { "iris", "digits", "mnist" };

      foreach (var dataset in expectedDatasets)
      {
        var datasetPath = Path.Combine(inputDir, dataset, "input.parquet");
        if (!File.Exists(datasetPath))
        {
          // Missing required dataset
          return false;
        }
      }

      // All required datasets exist
      return true;
    }

    // All other examples have their data checked in or generate it
    return true;
  }

  /// <summary>
  /// Finds the project directory for a given project name.
  /// Searches recursively under the examples directory to support subdirectories.
  /// </summary>
  private static string? FindProjectDirectory(string projectName)
  {
    var directories = Directory.GetDirectories(
      _examplesDirectory,
      projectName,
      SearchOption.AllDirectories
    );
    return directories.FirstOrDefault();
  }

  /// <summary>
  /// Checks if a project is an executable (has OutputType=Exe).
  /// </summary>
  private static bool IsExecutableProject(string csprojPath)
  {
    try
    {
      var content = File.ReadAllText(csprojPath);
      // Simple heuristic: check for <OutputType>Exe</OutputType>
      return content.Contains("<OutputType>Exe</OutputType>", StringComparison.OrdinalIgnoreCase);
    }
    catch
    {
      return false;
    }
  }

  /// <summary>
  /// Finds the entry point Type (Program class) for a given assembly.
  /// </summary>
  private static Type? FindEntryPointType(Assembly assembly, string projectName)
  {
    try
    {
      // Look for a type named "Program" - could be in the project namespace or compiler-generated
      var allTypes = assembly.GetTypes();
      var programTypes = allTypes.Where(t => t.Name == "Program").ToList();

      if (programTypes.Count == 0)
      {
        Console.WriteLine($"  No Program types found in assembly {projectName}");
        return null;
      }

      // Prefer a Program class in the project's namespace
      var explicitProgram = programTypes.FirstOrDefault(t =>
        t.Namespace?.StartsWith(projectName, StringComparison.OrdinalIgnoreCase) == true
      );

      if (explicitProgram != null)
      {
        return explicitProgram;
      }

      // Fall back to any Program class (e.g., compiler-generated from top-level statements)
      return programTypes.FirstOrDefault();
    }
    catch (Exception ex)
    {
      Console.WriteLine($"  Exception finding entry point for {projectName}: {ex.Message}");
      return null;
    }
  }

  /// <summary>
  /// Gets the workspace root directory by walking up from the current directory.
  /// </summary>
  private static string GetWorkspaceRoot()
  {
    var currentDir = Directory.GetCurrentDirectory();
    while (currentDir != null)
    {
      // Look for nx.json as a marker for the workspace root
      if (File.Exists(Path.Combine(currentDir, "nx.json")))
      {
        return currentDir;
      }

      currentDir = Directory.GetParent(currentDir)?.FullName;
    }

    throw new InvalidOperationException("Could not find workspace root (nx.json not found)");
  }
}
