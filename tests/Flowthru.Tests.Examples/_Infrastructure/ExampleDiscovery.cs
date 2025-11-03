using System.Reflection;

namespace Flowthru.Tests.Examples.Infrastructure;

/// <summary>
/// Discovers Flowthru example projects from the examples directory.
/// </summary>
public static class ExampleDiscovery
{
  private static readonly string _workspaceRoot = GetWorkspaceRoot();
  private static readonly string _examplesDirectory = Path.Combine(_workspaceRoot, "examples");

  // Explicit type references to force assembly loading
  // These must match the ProjectReferences in the .csproj file
  private static readonly Type[] _knownExampleTypes =
  [
    typeof(KedroSpaceflights.Custom.Program),
    typeof(KedroSpaceflights.Pure.Program),
    typeof(RetailData.Program),
  ];

  /// <summary>
  /// Discovers all executable example projects in the examples directory.
  /// </summary>
  /// <returns>A collection of discovered example projects.</returns>
  public static IEnumerable<ExampleProject> DiscoverExamples()
  {
    if (!Directory.Exists(_examplesDirectory))
    {
      throw new DirectoryNotFoundException($"Examples directory not found: {_examplesDirectory}");
    }

    // Use the known example types to ensure assemblies are loaded
    foreach (var exampleType in _knownExampleTypes)
    {
      var assembly = exampleType.Assembly;
      var projectName = assembly.GetName().Name!;
      var projectDir = FindProjectDirectory(projectName);

      if (projectDir == null)
      {
        Console.WriteLine($"Warning: Could not find project directory for {projectName}");
        continue;
      }

      yield return new ExampleProject
      {
        Name = projectName,
        ProjectPath = projectDir,
        CsprojPath = Path.Combine(projectDir, $"{projectName}.csproj"),
        EntryPointType = exampleType,
      };
    }
  }

  /// <summary>
  /// Finds the project directory for a given project name.
  /// </summary>
  private static string? FindProjectDirectory(string projectName)
  {
    var projectDir = Path.Combine(_examplesDirectory, projectName);
    return Directory.Exists(projectDir) ? projectDir : null;
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
  /// Finds the entry point Type (Program class) for a given project name.
  /// </summary>
  private static Type? FindEntryPointType(string projectName)
  {
    try
    {
      // The assembly should be loaded since we have ProjectReferences
      var assemblies = AppDomain.CurrentDomain.GetAssemblies();
      var assembly = assemblies.FirstOrDefault(a =>
        a.GetName().Name?.Equals(projectName, StringComparison.OrdinalIgnoreCase) == true
      );

      if (assembly == null)
      {
        Console.WriteLine($"  Assembly not found for: {projectName}");
        Console.WriteLine(
          $"  Available assemblies: {string.Join(", ", assemblies.Select(a => a.GetName().Name).Take(10))}..."
        );
        return null;
      }

      // Look for a type named "Program" - could be in the project namespace or compiler-generated
      var allTypes = assembly.GetTypes();
      var programTypes = allTypes.Where(t => t.Name == "Program").ToList();

      if (programTypes.Count == 0)
      {
        Console.WriteLine($"  No Program types found in assembly {projectName}");
        Console.WriteLine(
          $"  Available types: {string.Join(", ", allTypes.Select(t => t.FullName ?? t.Name).Take(5))}..."
        );
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
