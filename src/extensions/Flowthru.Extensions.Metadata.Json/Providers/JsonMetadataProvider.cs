using System.Text.Json;
using System.Text.Json.Serialization;
using Flowthru.Core.Graph.Meta.Models;
using Flowthru.Core.Meta;
using Flowthru.Core.Meta.Providers;
using Microsoft.Extensions.Logging;

namespace Flowthru.Meta.Providers;

/// <summary>
/// Exports DAG metadata as JSON files, and optionally exports post-run execution results.
/// </summary>
/// <remarks>
/// This provider creates timestamped JSON files containing the complete DAG structure
/// (nodes, catalog entries, edges, schema information). When post-run metadata is enabled,
/// it additionally exports a combined run result file containing both the DAG structure
/// and per-step execution outcomes.
/// </remarks>
[MetadataProviderBuilder(typeof(JsonMetadataProviderBuilder))]
public class JsonMetadataProvider : IMetadataProvider, IPostRunMetadataProvider
{
  private readonly bool _useCompactFormat;
  private readonly string _outputDirectory;
  private readonly string _dagFilenameTemplate;
  private readonly string _runFilenameTemplate;
  private readonly TimestampConfiguration _timestampConfig;
  private readonly ILogger? _logger;

  private static readonly JsonSerializerOptions _jsonOptions =
    new()
    {
      PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
      WriteIndented = true,
      DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
      Converters =
      {
        new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
        new ExceptionJsonConverter(),
      },
    };

  private static readonly JsonSerializerOptions _compactJsonOptions =
    new(_jsonOptions) { WriteIndented = false };

  /// <summary>
  /// Initializes a new JSON metadata provider.
  /// </summary>
  public JsonMetadataProvider(
    string outputDirectory,
    string dagFilenameTemplate,
    string runFilenameTemplate,
    TimestampConfiguration timestampConfig,
    bool useCompactFormat = false,
    ILogger? logger = null
  )
  {
    _outputDirectory = outputDirectory ?? throw new ArgumentNullException(nameof(outputDirectory));
    _dagFilenameTemplate =
      dagFilenameTemplate ?? throw new ArgumentNullException(nameof(dagFilenameTemplate));
    _runFilenameTemplate =
      runFilenameTemplate ?? throw new ArgumentNullException(nameof(runFilenameTemplate));
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
      Directory.CreateDirectory(_outputDirectory);

      var timestamp = _timestampConfig.GenerateTimestamp();
      var filename = FilenameTemplateParser.Render(dag, _dagFilenameTemplate, timestamp) + ".json";
      var filePath = Path.Combine(_outputDirectory, filename);

      _logger?.LogInformation("Exporting JSON DAG metadata to {FilePath}", filePath);

      var json = _useCompactFormat
        ? JsonSerializer.Serialize(dag, _compactJsonOptions)
        : JsonSerializer.Serialize(dag, _jsonOptions);

      AtomicWriteFile(filePath, json);

      _logger?.LogInformation(
        "Successfully exported JSON DAG metadata ({Steps} nodes, {Entries} catalog entries, {Edges} edges)",
        dag.Steps.Count,
        dag.CatalogItems.Count,
        dag.Edges.Count
      );
    }
    catch (Exception ex)
    {
      _logger?.LogWarning(
        ex,
        "Failed to export JSON DAG metadata to {OutputDirectory}",
        _outputDirectory
      );
    }
  }

  /// <inheritdoc />
  public void Consume(RunMetadata run)
  {
    try
    {
      Directory.CreateDirectory(_outputDirectory);

      var timestamp = _timestampConfig.GenerateTimestamp();
      var filename =
        FilenameTemplateParser.Render(run.Dag, _runFilenameTemplate, timestamp) + ".json";
      var filePath = Path.Combine(_outputDirectory, filename);

      _logger?.LogInformation("Exporting JSON run metadata to {FilePath}", filePath);

      var json = _useCompactFormat
        ? JsonSerializer.Serialize(run, _compactJsonOptions)
        : JsonSerializer.Serialize(run, _jsonOptions);

      AtomicWriteFile(filePath, json);

      _logger?.LogInformation(
        "Successfully exported JSON run metadata (success={Success}, elapsed={Elapsed}ms)",
        run.Result.Success,
        run.Result.ExecutionTime.TotalMilliseconds
      );
    }
    catch (Exception ex)
    {
      _logger?.LogWarning(
        ex,
        "Failed to export JSON run metadata to {OutputDirectory}",
        _outputDirectory
      );
    }
  }

  /// <summary>
  /// Writes content to a file atomically using a temp-then-rename pattern.
  /// </summary>
  private static void AtomicWriteFile(string filePath, string content)
  {
    var tempPath = filePath + ".tmp";

    try
    {
      File.WriteAllText(tempPath, content);

      if (File.Exists(filePath))
      {
        File.Delete(filePath);
      }

      File.Move(tempPath, filePath);
    }
    finally
    {
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

  /// <summary>
  /// Converts Exception to a JSON-serializable summary, avoiding circular reference issues
  /// that arise from serializing Exception directly.
  /// </summary>
  private sealed class ExceptionJsonConverter : JsonConverter<Exception>
  {
    public override Exception? Read(
      ref Utf8JsonReader reader,
      Type typeToConvert,
      JsonSerializerOptions options
    ) => null;

    public override void Write(
      Utf8JsonWriter writer,
      Exception value,
      JsonSerializerOptions options
    )
    {
      writer.WriteStartObject();
      writer.WriteString("type", value.GetType().Name);
      writer.WriteString("message", value.Message);

      if (value.InnerException != null)
      {
        writer.WritePropertyName("innerException");
        Write(writer, value.InnerException, options);
      }

      writer.WriteEndObject();
    }
  }
}
