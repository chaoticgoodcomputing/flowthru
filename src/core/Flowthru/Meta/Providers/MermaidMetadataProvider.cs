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
public class MermaidMetadataProvider : IMetadataProvider
{
  private readonly MermaidFlowchartDirection _direction;
  private readonly string _activeNodeColor;
  private readonly string _activeDataColor;

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
  /// <param name="direction">Flow direction for the diagram</param>
  /// <param name="activeNodeColor">Hex color for active (sliced) nodes</param>
  /// <param name="activeDataColor">Hex color for active (sliced) catalog entries</param>
  public MermaidMetadataProvider(
    MermaidFlowchartDirection direction = MermaidFlowchartDirection.TopToBottom,
    string activeNodeColor = "#2E7D32",
    string activeDataColor = "#2E7D32"
  )
  {
    _direction = direction;
    _activeNodeColor = activeNodeColor;
    _activeDataColor = activeDataColor;
  }

  /// <inheritdoc />
  public string Name => "Mermaid";

  /// <inheritdoc />
  public bool Export(
    DagMetadata dag,
    string outputDirectory,
    string filenameTemplate,
    TimestampConfiguration timestampConfig,
    ILogger? logger = null
  )
  {
    try
    {
      // Ensure output directory exists
      Directory.CreateDirectory(outputDirectory);

      // Generate filename from template
      var timestamp = timestampConfig.GenerateTimestamp();
      var filename = FilenameTemplateParser.Render(dag, filenameTemplate, timestamp) + ".md";
      var filePath = Path.Combine(outputDirectory, filename);

      logger?.LogInformation("Exporting Mermaid diagram to {FilePath}", filePath);

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

        logger?.LogInformation("Successfully exported Mermaid diagram");

        return true;
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
      logger?.LogWarning(
        ex,
        "Failed to export Mermaid diagram to {OutputDirectory}",
        outputDirectory
      );
      return false;
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
