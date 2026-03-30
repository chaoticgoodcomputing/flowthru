using Flowthru.Meta.Models;
using Microsoft.Extensions.Logging;

namespace Flowthru.Meta.Providers;

/// <summary>
/// Exports DAG metadata as Mermaid flowchart diagrams.
/// </summary>
/// <remarks>
/// This provider creates Markdown files containing Mermaid flowchart diagrams
/// for immediate visualization in GitHub, VS Code, and other Mermaid-compatible viewers.
/// </remarks>
[MetadataProviderBuilder(typeof(MermaidMetadataProviderBuilder))]
public class MermaidMetadataProvider : IMetadataProvider
{
  private readonly MermaidFlowchartDirection _direction;
  private readonly string _activeNodeColor;
  private readonly string _activeDataColor;
  private readonly string _outputDirectory;
  private readonly string _filenameTemplate;
  private readonly TimestampConfiguration _timestampConfig;
  private readonly ILogger? _logger;

  /// <summary>
  /// Flow direction for Mermaid flowcharts.
  /// </summary>
  public enum MermaidFlowchartDirection
  {
    /// <summary>Top to Bottom (default)</summary>
    TopToBottom,

    /// <summary>Left to Right</summary>
    LeftToRight,

    /// <summary>Bottom to Top</summary>
    BottomToTop,

    /// <summary>Right to Left</summary>
    RightToLeft,
  }

  /// <summary>
  /// Initializes a new Mermaid metadata provider.
  /// </summary>
  /// <param name="outputDirectory">Directory to write Mermaid files to</param>
  /// <param name="filenameTemplate">Template for generating output filenames</param>
  /// <param name="timestampConfig">Configuration for timestamp handling in filenames</param>
  /// <param name="direction">Flow direction for the diagram</param>
  /// <param name="activeNodeColor">Hex color for active (sliced) nodes</param>
  /// <param name="activeDataColor">Hex color for active (sliced) catalog entries</param>
  /// <param name="logger">Optional logger for diagnostic messages</param>
  public MermaidMetadataProvider(
    string outputDirectory,
    string filenameTemplate,
    TimestampConfiguration timestampConfig,
    MermaidFlowchartDirection direction = MermaidFlowchartDirection.TopToBottom,
    string activeNodeColor = "#2E7D32",
    string activeDataColor = "#2E7D32",
    ILogger? logger = null
  )
  {
    _outputDirectory = outputDirectory ?? throw new ArgumentNullException(nameof(outputDirectory));
    _filenameTemplate =
      filenameTemplate ?? throw new ArgumentNullException(nameof(filenameTemplate));
    _timestampConfig = timestampConfig ?? throw new ArgumentNullException(nameof(timestampConfig));
    _direction = direction;
    _activeNodeColor = activeNodeColor;
    _activeDataColor = activeDataColor;
    _logger = logger;
  }

  /// <inheritdoc />
  public string Name => "Mermaid";

  /// <inheritdoc />
  public void Consume(DagMetadata dag)
  {
    try
    {
      // Ensure output directory exists
      Directory.CreateDirectory(_outputDirectory);

      // Generate filename from template
      var timestamp = _timestampConfig.GenerateTimestamp();
      var filename = FilenameTemplateParser.Render(dag, _filenameTemplate, timestamp) + ".md";
      var filePath = Path.Combine(_outputDirectory, filename);

      _logger?.LogInformation("Exporting Mermaid diagram to {FilePath}", filePath);

      // Generate Mermaid diagram with configured direction and colors
      var mermaid = dag.ToMermaidDiagram(
        GetDirectionCode(_direction),
        _activeNodeColor,
        _activeDataColor
      );

      // Atomic write: write to temp file first, then rename
      var tempPath = filePath + ".tmp";

      try
      {
        File.WriteAllText(tempPath, mermaid);

        // Rename temp file to final name
        if (File.Exists(filePath))
        {
          File.Delete(filePath);
        }
        File.Move(tempPath, filePath);

        _logger?.LogInformation("Successfully exported Mermaid diagram");
      }
      finally
      {
        // Clean up temp file if it still exists
        if (File.Exists(tempPath))
        {
          try
          {
            File.Delete(tempPath);
          }
          catch
          {
            // Ignore cleanup errors
          }
        }
      }
    }
    catch (Exception ex)
    {
      _logger?.LogWarning(
        ex,
        "Failed to export Mermaid diagram to {OutputDirectory}",
        _outputDirectory
      );
    }
  }

  /// <summary>
  /// Converts flow direction enum to Mermaid direction code.
  /// </summary>
  private static string GetDirectionCode(MermaidFlowchartDirection direction)
  {
    return direction switch
    {
      MermaidFlowchartDirection.TopToBottom => "TB",
      MermaidFlowchartDirection.LeftToRight => "LR",
      MermaidFlowchartDirection.BottomToTop => "BT",
      MermaidFlowchartDirection.RightToLeft => "RL",
      _ => "TB",
    };
  }
}
