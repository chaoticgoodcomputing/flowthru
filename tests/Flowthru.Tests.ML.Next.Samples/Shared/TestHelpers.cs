using System.Reflection;

namespace Flowthru.Tests.ML.Next.Samples.Shared;

/// <summary>
/// Helper utilities for test data path resolution, matching ML.NET sample patterns.
/// </summary>
public static class TestHelpers
{
  /// <summary>
  /// Get absolute path to a data file relative to the test assembly output directory.
  /// Matches the pattern used in ML.NET samples.
  /// </summary>
  /// <param name="relativePath">Relative path from test output directory (e.g., "Clustering_Iris/Data/iris-full.txt")</param>
  /// <returns>Absolute path to the data file</returns>
  public static string GetDataPath(string relativePath)
  {
    var assemblyLocation = typeof(TestHelpers).Assembly.Location;
    var assemblyDirectory = Path.GetDirectoryName(assemblyLocation)
        ?? throw new InvalidOperationException("Could not determine assembly directory");

    return Path.Combine(assemblyDirectory, relativePath);
  }

  /// <summary>
  /// Verify that a data file exists at the expected path.
  /// Useful for debugging data file copy issues.
  /// </summary>
  /// <param name="relativePath">Relative path to check</param>
  /// <exception cref="FileNotFoundException">Thrown if file doesn't exist</exception>
  public static void VerifyDataFileExists(string relativePath)
  {
    var fullPath = GetDataPath(relativePath);
    if (!File.Exists(fullPath))
    {
      throw new FileNotFoundException(
          $"Data file not found. Expected at: {fullPath}. " +
          $"Ensure the file is marked with <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory> in the .csproj",
          fullPath);
    }
  }
}
