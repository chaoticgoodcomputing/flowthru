using Flowthru.Meta.Models;
using Microsoft.Extensions.Logging;

namespace Flowthru.Meta.Persistence;

/// <summary>
/// Handles persistence of DAG metadata to JSON files.
/// </summary>
/// <remarks>
/// <para>
/// This class manages writing DAG metadata to disk with proper error handling,
/// atomic writes, and directory management. Files are timestamped to maintain
/// a history of pipeline builds.
/// </para>
/// <para>
/// <strong>Filename Format:</strong> <c>dag-{pipelineName}-{timestamp}.json</c>
/// </para>
/// <para>
/// Example: <c>dag-DataProcessing-20251024-143052.json</c>
/// </para>
/// </remarks>
public static class DagPersistence {
  /// <summary>
  /// Saves DAG metadata to a JSON file in the specified directory.
  /// </summary>
  /// <param name="metadata">The DAG metadata to save</param>
  /// <param name="outputDirectory">Directory to write the JSON file to</param>
  /// <param name="logger">Optional logger for diagnostic messages</param>
  /// <param name="saveMermaid">Whether to also save a Mermaid diagram file (.md)</param>
  /// <returns>The path to the created JSON file, or null if save failed</returns>
  /// <remarks>
  /// <para>
  /// This method:
  /// </para>
  /// <list type="number">
  /// <item>Creates the output directory if it doesn't exist</item>
  /// <item>Generates a timestamped filename</item>
  /// <item>Writes to a temporary file first (atomic write)</item>
  /// <item>Renames temp file to final name (prevents corruption)</item>
  /// <item>Optionally saves a Mermaid diagram file alongside JSON</item>
  /// <item>Handles errors gracefully (logs warnings, doesn't throw)</item>
  /// </list>
  /// <para>
  /// <strong>Error Handling:</strong> Failures are logged as warnings but don't throw exceptions.
  /// This ensures metadata export problems never crash pipeline execution.
  /// </para>
  /// </remarks>
  public static string? SaveDag(DagMetadata metadata, string outputDirectory, ILogger? logger = null, bool saveMermaid = true) {
    if (metadata == null) {
      throw new ArgumentNullException(nameof(metadata));
    }

    if (string.IsNullOrWhiteSpace(outputDirectory)) {
      throw new ArgumentException("Output directory cannot be null or empty", nameof(outputDirectory));
    }

    try {
      // Ensure output directory exists
      Directory.CreateDirectory(outputDirectory);

      // Generate timestamped filename
      var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
      var filename = $"dag-{SanitizeFilename(metadata.PipelineName)}-{timestamp}.json";
      var filePath = Path.Combine(outputDirectory, filename);

      logger?.LogInformation("Exporting DAG metadata to {FilePath}", filePath);

      // Serialize to JSON
      var json = metadata.ToJson();

      // Atomic write: write to temp file first, then rename
      var tempPath = filePath + ".tmp";

      try {
        File.WriteAllText(tempPath, json);

        // Rename temp file to final name (atomic operation on most filesystems)
        if (File.Exists(filePath)) {
          File.Delete(filePath);
        }
        File.Move(tempPath, filePath);

        logger?.LogInformation("Successfully exported DAG metadata ({Nodes} nodes, {Entries} catalog entries, {Edges} edges)",
          metadata.Nodes.Count,
          metadata.CatalogEntries.Count,
          metadata.Edges.Count);

        // Save Mermaid diagram if requested
        if (saveMermaid) {
          SaveMermaidDiagram(metadata, outputDirectory, timestamp, logger);
        }

        return filePath;
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
      // Log error but don't throw - metadata export should never crash pipeline execution
      logger?.LogWarning(ex, "Failed to export DAG metadata to {OutputDirectory}", outputDirectory);
      return null;
    }
  }

  /// <summary>
  /// Sanitizes a pipeline name for use in a filename.
  /// </summary>
  /// <remarks>
  /// Replaces invalid filename characters with underscores.
  /// Example: "Data/Processing" → "Data_Processing"
  /// </remarks>
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
  /// Saves a Mermaid diagram representation of the DAG to a Markdown file.
  /// </summary>
  /// <param name="metadata">The DAG metadata to visualize</param>
  /// <param name="outputDirectory">Directory to write the Markdown file to</param>
  /// <param name="timestamp">Timestamp string for the filename</param>
  /// <param name="logger">Optional logger for diagnostic messages</param>
  /// <remarks>
  /// Creates a .md file containing a Mermaid code fence with the DAG visualization.
  /// Uses the same filename pattern as JSON files but with .md extension.
  /// </remarks>
  private static void SaveMermaidDiagram(DagMetadata metadata, string outputDirectory, string timestamp, ILogger? logger = null) {
    try {
      var filename = $"dag-{SanitizeFilename(metadata.PipelineName)}-{timestamp}.md";
      var filePath = Path.Combine(outputDirectory, filename);

      logger?.LogInformation("Exporting Mermaid diagram to {FilePath}", filePath);

      // Generate Mermaid diagram
      var mermaid = metadata.ToMermaidDiagram();

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
      // Log error but don't throw - Mermaid export failure shouldn't crash anything
      logger?.LogWarning(ex, "Failed to export Mermaid diagram to {OutputDirectory}", outputDirectory);
    }
  }
}
