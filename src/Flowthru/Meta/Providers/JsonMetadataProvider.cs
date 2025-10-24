using Flowthru.Meta.Models;
using Microsoft.Extensions.Logging;

namespace Flowthru.Meta.Providers;

/// <summary>
/// Exports DAG metadata as JSON files.
/// </summary>
/// <remarks>
/// This provider creates timestamped JSON files containing the complete DAG structure
/// (nodes, catalog entries, edges, schema information) for consumption by Flowthru.Viz
/// or other visualization tools.
/// </remarks>
public class JsonMetadataProvider : IMetadataProvider {
  private readonly bool _useCompactFormat;

  /// <summary>
  /// Initializes a new JSON metadata provider.
  /// </summary>
  /// <param name="useCompactFormat">Whether to use compact (minified) JSON format</param>
  public JsonMetadataProvider(bool useCompactFormat = false) {
    _useCompactFormat = useCompactFormat;
  }

  /// <inheritdoc />
  public string Name => "JSON";

  /// <inheritdoc />
  public bool Export(DagMetadata dag, string outputDirectory, ILogger? logger = null) {
    try {
      // Ensure output directory exists
      Directory.CreateDirectory(outputDirectory);

      // Generate timestamped filename
      var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
      var filename = $"dag-{SanitizeFilename(dag.PipelineName)}-{timestamp}.json";
      var filePath = Path.Combine(outputDirectory, filename);

      logger?.LogInformation("Exporting JSON metadata to {FilePath}", filePath);

      // Serialize to JSON
      var json = _useCompactFormat ? dag.ToCompactJson() : dag.ToJson();

      // Atomic write: write to temp file first, then rename
      var tempPath = filePath + ".tmp";

      try {
        File.WriteAllText(tempPath, json);

        // Rename temp file to final name (atomic operation on most filesystems)
        if (File.Exists(filePath)) {
          File.Delete(filePath);
        }
        File.Move(tempPath, filePath);

        logger?.LogInformation("Successfully exported JSON metadata ({Nodes} nodes, {Entries} catalog entries, {Edges} edges)",
          dag.Nodes.Count,
          dag.CatalogEntries.Count,
          dag.Edges.Count);

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
      logger?.LogWarning(ex, "Failed to export JSON metadata to {OutputDirectory}", outputDirectory);
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
}
