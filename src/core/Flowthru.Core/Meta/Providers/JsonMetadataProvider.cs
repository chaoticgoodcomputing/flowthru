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
[MetadataProviderBuilder(typeof(JsonMetadataProviderBuilder))]
public class JsonMetadataProvider : IMetadataProvider
{
  private readonly bool _useCompactFormat;
  private readonly string _outputDirectory;
  private readonly string _filenameTemplate;
  private readonly TimestampConfiguration _timestampConfig;
  private readonly ILogger? _logger;

  /// <summary>
  /// Initializes a new JSON metadata provider.
  /// </summary>
  /// <param name="outputDirectory">Directory to write JSON files to</param>
  /// <param name="filenameTemplate">Template for generating output filenames</param>
  /// <param name="timestampConfig">Configuration for timestamp handling in filenames</param>
  /// <param name="useCompactFormat">Whether to use compact (minified) JSON format</param>
  /// <param name="logger">Optional logger for diagnostic messages</param>
  public JsonMetadataProvider(
    string outputDirectory,
    string filenameTemplate,
    TimestampConfiguration timestampConfig,
    bool useCompactFormat = false,
    ILogger? logger = null
  )
  {
    _outputDirectory = outputDirectory ?? throw new ArgumentNullException(nameof(outputDirectory));
    _filenameTemplate =
      filenameTemplate ?? throw new ArgumentNullException(nameof(filenameTemplate));
    _timestampConfig = timestampConfig ?? throw new ArgumentNullException(nameof(timestampConfig));
    _useCompactFormat = useCompactFormat;
    _logger = logger;
  }

  /// <inheritdoc />
  public string Name => "JSON";

  /// <inheritdoc />
  public void Consume(DagMetadata dag)
  {
    try
    {
      // Ensure output directory exists
      Directory.CreateDirectory(_outputDirectory);

      // Generate filename from template
      var timestamp = _timestampConfig.GenerateTimestamp();
      var filename = FilenameTemplateParser.Render(dag, _filenameTemplate, timestamp) + ".json";
      var filePath = Path.Combine(_outputDirectory, filename);

      _logger?.LogInformation("Exporting JSON metadata to {FilePath}", filePath);

      // Serialize to JSON
      var json = _useCompactFormat ? dag.ToCompactJson() : dag.ToJson();

      // Atomic write: write to temp file first, then rename
      var tempPath = filePath + ".tmp";

      try
      {
        File.WriteAllText(tempPath, json);

        // Rename temp file to final name (atomic operation on most filesystems)
        if (File.Exists(filePath))
        {
          File.Delete(filePath);
        }
        File.Move(tempPath, filePath);

        _logger?.LogInformation(
          "Successfully exported JSON metadata ({Nodes} nodes, {Entries} catalog entries, {Edges} edges)",
          dag.Nodes.Count,
          dag.CatalogEntries.Count,
          dag.Edges.Count
        );
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
        "Failed to export JSON metadata to {OutputDirectory}",
        _outputDirectory
      );
    }
  }
}
