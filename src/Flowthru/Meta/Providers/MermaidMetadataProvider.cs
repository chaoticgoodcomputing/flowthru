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
public class MermaidMetadataProvider : IMetadataProvider {
  private readonly MermaidFlowchartDirection _direction;

  /// <summary>
  /// Flow direction for Mermaid flowcharts.
  /// </summary>
  public enum MermaidFlowchartDirection {
    /// <summary>Top to Bottom (default)</summary>
    TopToBottom,
    /// <summary>Left to Right</summary>
    LeftToRight,
    /// <summary>Bottom to Top</summary>
    BottomToTop,
    /// <summary>Right to Left</summary>
    RightToLeft
  }

  /// <summary>
  /// Initializes a new Mermaid metadata provider.
  /// </summary>
  /// <param name="direction">Flow direction for the diagram</param>
  public MermaidMetadataProvider(MermaidFlowchartDirection direction = MermaidFlowchartDirection.TopToBottom) {
    _direction = direction;
  }

  /// <inheritdoc />
  public string Name => "Mermaid";

  /// <inheritdoc />
  public bool Export(DagMetadata dag, string outputDirectory, ILogger? logger = null) {
    try {
      // Ensure output directory exists
      Directory.CreateDirectory(outputDirectory);

      // Generate timestamped filename
      var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
      var filename = $"dag-{SanitizeFilename(dag.PipelineName)}-{timestamp}.md";
      var filePath = Path.Combine(outputDirectory, filename);

      logger?.LogInformation("Exporting Mermaid diagram to {FilePath}", filePath);

      // Generate Mermaid diagram with configured direction
      var mermaid = dag.ToMermaidDiagram(GetDirectionCode(_direction));

      // Atomic write: write to temp file first, then rename
      var tempPath = filePath + ".tmp";

      try {
        File.WriteAllText(tempPath, mermaid);

        // Rename temp file to final name
        if (File.Exists(filePath)) {
          File.Delete(filePath);
        }
        File.Move(tempPath, filePath);

        logger?.LogInformation("Successfully exported Mermaid diagram");

        return true;
      } finally {
        // Clean up temp file if it still exists
        if (File.Exists(tempPath)) {
          try {
            File.Delete(tempPath);
          } catch {
            // Ignore cleanup errors
          }
        }
      }
    } catch (Exception ex) {
      logger?.LogWarning(ex, "Failed to export Mermaid diagram to {OutputDirectory}", outputDirectory);
      return false;
    }
  }

  /// <summary>
  /// Sanitizes a pipeline name for use in a filename.
  /// </summary>
  private static string SanitizeFilename(string name) {
    if (string.IsNullOrWhiteSpace(name)) {
      return "UnnamedPipeline";
    }

    var invalidChars = Path.GetInvalidFileNameChars();
    var sanitized = name;

    foreach (var c in invalidChars) {
      sanitized = sanitized.Replace(c, '_');
    }

    // Also replace spaces with underscores for cleaner filenames
    sanitized = sanitized.Replace(' ', '_');

    return sanitized;
  }

  /// <summary>
  /// Converts flow direction enum to Mermaid direction code.
  /// </summary>
  private static string GetDirectionCode(MermaidFlowchartDirection direction) {
    return direction switch {
      MermaidFlowchartDirection.TopToBottom => "TB",
      MermaidFlowchartDirection.LeftToRight => "LR",
      MermaidFlowchartDirection.BottomToTop => "BT",
      MermaidFlowchartDirection.RightToLeft => "RL",
      _ => "TB"
    };
  }
}
