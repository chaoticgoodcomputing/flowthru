using System.Text.Json;
using System.Text.Json.Serialization;

namespace Flowthru.Tests.Templates.Infrastructure;

/// <summary>
/// Discovers available Flowthru templates by scanning per-starter .template.config/template.json files.
/// Adding a new starter under examples/starter/ with its own .template.config/template.json
/// automatically registers it here — no central registry required.
/// </summary>
public static class TemplateDiscovery
{
  private static readonly string _workspaceRoot = GetWorkspaceRoot();
  private static readonly string _starterRoot = Path.Combine(_workspaceRoot, "examples", "starter");

  /// <summary>
  /// Discovers all available starter templates by scanning per-starter template.json files.
  /// </summary>
  public static IEnumerable<TemplateProject> DiscoverTemplates()
  {
    if (!Directory.Exists(_starterRoot))
    {
      throw new DirectoryNotFoundException($"Starter directory not found: {_starterRoot}");
    }

    var templateConfigs = Directory
      .GetFiles(_starterRoot, "template.json", SearchOption.AllDirectories)
      .Where(p => p.Contains(Path.Combine(".template.config", "template.json")));

    var testOutputPath = Path.Combine(Path.GetTempPath(), "flowthru-template-tests");
    var found = false;

    foreach (var configPath in templateConfigs)
    {
      found = true;
      var configJson = File.ReadAllText(configPath);
      var config = JsonSerializer.Deserialize<TemplateConfig>(configJson);

      var shortName = config?.ShortName;
      if (string.IsNullOrWhiteSpace(shortName))
      {
        throw new InvalidOperationException(
          $"template.json at '{configPath}' is missing a 'shortName'."
        );
      }

      // Derive a short unique project name from the shortName (e.g. "Flowthru.Iris" → "IrisXXXX")
      var slug = shortName.Contains('.')
        ? shortName[(shortName.LastIndexOf('.') + 1)..]
        : shortName;
      var projectName = $"{slug}{Guid.NewGuid():N}"[..20];

      yield return new TemplateProject
      {
        StarterName = shortName,
        ProjectName = projectName,
        GeneratedPath = Path.Combine(testOutputPath, projectName),
        PipelineName = null,
      };
    }

    if (!found)
    {
      throw new InvalidOperationException(
        $"No template.json files found under '{_starterRoot}'. "
          + "Each starter must have a .template.config/template.json."
      );
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
      if (File.Exists(Path.Combine(currentDir, "nx.json")))
      {
        return currentDir;
      }

      currentDir = Directory.GetParent(currentDir)?.FullName;
    }

    throw new InvalidOperationException("Could not find workspace root (nx.json not found)");
  }

  #region JSON Models

  private class TemplateConfig
  {
    [JsonPropertyName("shortName")]
    public string? ShortName { get; set; }
  }

  #endregion
}
