using System.Text.Json;
using System.Text.Json.Serialization;

namespace Flowthru.Tests.Templates.Infrastructure;

/// <summary>
/// Discovers available Flowthru templates from the template.json configuration.
/// </summary>
public static class TemplateDiscovery
{
  private static readonly string _workspaceRoot = GetWorkspaceRoot();
  private static readonly string _templateConfigPath = Path.Combine(
    _workspaceRoot,
    "examples",
    "starter",
    ".template.config",
    "template.json"
  );

  /// <summary>
  /// Discovers all available starter templates.
  /// </summary>
  public static IEnumerable<TemplateProject> DiscoverTemplates()
  {
    if (!File.Exists(_templateConfigPath))
    {
      throw new FileNotFoundException($"Template config not found: {_templateConfigPath}");
    }

    var configJson = File.ReadAllText(_templateConfigPath);
    var config = JsonSerializer.Deserialize<TemplateConfig>(configJson);

    if (config?.Symbols?.Starter?.Choices == null)
    {
      throw new InvalidOperationException("No starter choices found in template.json");
    }

    var testOutputPath = Path.Combine(Path.GetTempPath(), "flowthru-template-tests");

    foreach (var choice in config.Symbols.Starter.Choices)
    {
      var projectName = $"Test{ToPascalCase(choice.Choice)}{Guid.NewGuid():N}"[..20];

      yield return new TemplateProject
      {
        StarterName = choice.Choice,
        ProjectName = projectName,
        GeneratedPath = Path.Combine(testOutputPath, projectName),
        PipelineName = null, // Run entire project without specifying pipeline
      };
    }
  }

  /// <summary>
  /// Converts a string to PascalCase for project naming.
  /// </summary>
  private static string ToPascalCase(string input)
  {
    if (string.IsNullOrEmpty(input))
    {
      return input;
    }

    var words = input.Split('-', '_', ' ');
    return string.Concat(words.Select(w => char.ToUpper(w[0]) + w.Substring(1).ToLower()));
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
    [JsonPropertyName("symbols")]
    public SymbolsConfig? Symbols { get; set; }
  }

  private class SymbolsConfig
  {
    [JsonPropertyName("starter")]
    public StarterSymbol? Starter { get; set; }
  }

  private class StarterSymbol
  {
    [JsonPropertyName("choices")]
    public List<StarterChoice>? Choices { get; set; }
  }

  private class StarterChoice
  {
    [JsonPropertyName("choice")]
    public string Choice { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
  }

  #endregion
}
