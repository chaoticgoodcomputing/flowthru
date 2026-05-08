using System.Text.Json;
using System.Text.Json.Serialization;
using Flowthru.Diagnostics.Json.Internal;
using Flowthru.Flow;
using Flowthru.Prelude;
using Microsoft.Extensions.Logging;

namespace Flowthru.Diagnostics.Json;

/// <summary>
/// Emits Flowthru DAG / run metadata as JSON files. Implements both
/// <see cref="IMetadataProvider"/> (pre-run DAG manifest) and
/// <see cref="IPostRunMetadataProvider"/> (combined DAG + result
/// after the flow completes).
/// </summary>
/// <remarks>
/// <para>
/// <strong>Atomic writes.</strong> Each export writes to a temp file
/// first, then renames atomically over the target. Avoids producing
/// half-written manifests on crash.
/// </para>
/// <para>
/// <strong>Logger optional.</strong> The provider runs without a
/// logger (silent on success, surfaces failures via
/// <see cref="FlowIO{A}"/>). When a logger is supplied via
/// <see cref="JsonMetadataProviderBuilder.WithLogger"/>, the provider
/// logs export targets and outcomes at <c>Information</c> level.
/// </para>
/// </remarks>
public sealed class JsonMetadataProvider : IMetadataProvider, IPostRunMetadataProvider
{
  private readonly bool _useCompactFormat;
  private readonly string _outputDirectory;
  private readonly string _dagFilenameTemplate;
  private readonly string _runFilenameTemplate;
  private readonly TimestampConfiguration _timestampConfig;
  private readonly ILogger? _logger;

  private static readonly JsonSerializerOptions _indented = new()
  {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
  };

  private static readonly JsonSerializerOptions _compact = new(_indented)
  {
    WriteIndented = false,
  };

  internal JsonMetadataProvider(
    string outputDirectory,
    string dagFilenameTemplate,
    string runFilenameTemplate,
    TimestampConfiguration timestampConfig,
    bool useCompactFormat,
    ILogger? logger
  )
  {
    _outputDirectory = outputDirectory ?? throw new ArgumentNullException(nameof(outputDirectory));
    _dagFilenameTemplate = dagFilenameTemplate
      ?? throw new ArgumentNullException(nameof(dagFilenameTemplate));
    _runFilenameTemplate = runFilenameTemplate
      ?? throw new ArgumentNullException(nameof(runFilenameTemplate));
    _timestampConfig = timestampConfig ?? throw new ArgumentNullException(nameof(timestampConfig));
    _useCompactFormat = useCompactFormat;
    _logger = logger;
  }

  /// <inheritdoc/>
  public string ProviderId => "Flowthru.Json";

  /// <summary>
  /// The output directory provider exports files to. Resolved at
  /// construction time and held verbatim.
  /// </summary>
  public string OutputDirectory => _outputDirectory;

  /// <inheritdoc/>
  public FlowIO<FlowUnit> Emit(FlowMetadataContext ctx) =>
    FlowIO.LiftAsync(async ct =>
    {
      var dag = DagMetadataProjection.From(ctx);
      var timestamp = _timestampConfig.GenerateTimestamp();
      var filename = FilenameTemplateParser.Render(
        ctx.EffectiveFlow.Label, _dagFilenameTemplate, timestamp
      ) + ".json";
      var filePath = Path.Combine(_outputDirectory, filename);

      _logger?.LogInformation("Exporting JSON DAG metadata to {FilePath}", filePath);

      Directory.CreateDirectory(_outputDirectory);
      var json = JsonSerializer.Serialize(dag, _useCompactFormat ? _compact : _indented);
      await AtomicWriteFile(filePath, json, ct).ConfigureAwait(false);

      _logger?.LogInformation(
        "Exported JSON DAG metadata ({Steps} steps, {Items} items, {Edges} edges)",
        dag.Steps.Count,
        dag.CatalogItems.Count,
        dag.Edges.Count
      );

      return FlowUnit.Default;
    }, source: $"JsonMetadataProvider.Emit[Dag,{ctx.EffectiveFlow.Label}]");

  /// <inheritdoc/>
  public FlowIO<FlowUnit> Emit(FlowRunMetadataContext ctx) =>
    FlowIO.LiftAsync(async ct =>
    {
      var run = RunMetadataProjection.From(ctx);
      var timestamp = _timestampConfig.GenerateTimestamp();
      var filename = FilenameTemplateParser.Render(
        ctx.Static.EffectiveFlow.Label, _runFilenameTemplate, timestamp
      ) + ".json";
      var filePath = Path.Combine(_outputDirectory, filename);

      _logger?.LogInformation("Exporting JSON run metadata to {FilePath}", filePath);

      Directory.CreateDirectory(_outputDirectory);
      var json = JsonSerializer.Serialize(run, _useCompactFormat ? _compact : _indented);
      await AtomicWriteFile(filePath, json, ct).ConfigureAwait(false);

      _logger?.LogInformation(
        "Exported JSON run metadata (success={Success}, steps={Steps})",
        run.Result.Success,
        run.Result.StepResults.Count
      );

      return FlowUnit.Default;
    }, source: $"JsonMetadataProvider.Emit[Run,{ctx.Static.EffectiveFlow.Label}]");

  /// <summary>
  /// Atomic temp-then-rename write. Disposes the temp file on any
  /// failure path so partial writes never leak.
  /// </summary>
  private static async Task AtomicWriteFile(string filePath, string content, CancellationToken ct)
  {
    var tempPath = filePath + ".tmp";
    try
    {
      await File.WriteAllTextAsync(tempPath, content, ct).ConfigureAwait(false);
      if (File.Exists(filePath)) File.Delete(filePath);
      File.Move(tempPath, filePath);
    }
    finally
    {
      if (File.Exists(tempPath))
      {
        try { File.Delete(tempPath); }
        catch { /* best-effort cleanup */ }
      }
    }
  }
}
