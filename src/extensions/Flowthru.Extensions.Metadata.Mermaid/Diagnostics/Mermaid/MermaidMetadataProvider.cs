using Flowthru.Diagnostics.Mermaid.Internal;
using Flowthru.Flow;
using Flowthru.Prelude;
using Flowthru.Step;
using Microsoft.Extensions.Logging;

namespace Flowthru.Diagnostics.Mermaid;

/// <summary>
/// Emits Flowthru DAG / run metadata as Mermaid flowchart diagrams
/// inside Markdown files. Implements both
/// <see cref="IMetadataProvider"/> (pre-run DAG diagram) and
/// <see cref="IPostRunMetadataProvider"/> (post-run diagram with
/// step nodes coloured by execution outcome).
/// </summary>
/// <remarks>
/// <para>
/// <strong>Slice-aware rendering.</strong> The provider receives a
/// <see cref="FlowMetadataContext"/> that carries the merged DAG plus
/// the active slice. <see cref="MermaidMetadataProviderBuilder.WithShowFullDag"/>
/// (default <c>true</c>) keeps inactive nodes in the diagram with a
/// muted theme so the surrounding topology stays visible; setting it
/// to <c>false</c> filters inactive nodes out entirely. When the host
/// runs the merged DAG without slicing, every node is active either
/// way and the toggle has no visible effect.
/// </para>
/// <para>
/// <strong>Cross-flow edges.</strong> A merged DAG with steps from
/// multiple registered flows renders one Mermaid <c>subgraph</c> per
/// flow of origin; cross-flow edges (e.g. a Reporting step consuming
/// a DataScience output) draw at the top level so Mermaid resolves
/// them between subgraphs naturally.
/// </para>
/// <para>
/// <strong>Heat-map collapses to uniform green.</strong> The legacy
/// renderer drove a green→amber heat-map off per-step
/// <c>ExecutionTime</c>. The new <see cref="StepResult"/> closed sum
/// doesn't carry timing data, so all succeeded steps render in the
/// same colour. Restoring the heat-map is a Core-shape carryover.
/// </para>
/// </remarks>
public sealed class MermaidMetadataProvider : IMetadataProvider, IPostRunMetadataProvider
{
  private readonly string _outputDirectory;
  private readonly string _dagFilenameTemplate;
  private readonly string _runFilenameTemplate;
  private readonly TimestampConfiguration _timestampConfig;
  private readonly MermaidFlowchartDirection _direction;
  private readonly MermaidDiagramRenderer.Theme _theme;
  private readonly bool _showFullDag;
  private readonly PerFlowOptions _perFlow;
  private readonly ILogger? _logger;

  internal MermaidMetadataProvider(
    string outputDirectory,
    string dagFilenameTemplate,
    string runFilenameTemplate,
    TimestampConfiguration timestampConfig,
    MermaidFlowchartDirection direction,
    MermaidDiagramRenderer.Theme theme,
    bool showFullDag,
    PerFlowOptions perFlow,
    ILogger? logger
  )
  {
    _outputDirectory = outputDirectory ?? throw new ArgumentNullException(nameof(outputDirectory));
    _dagFilenameTemplate = dagFilenameTemplate
      ?? throw new ArgumentNullException(nameof(dagFilenameTemplate));
    _runFilenameTemplate = runFilenameTemplate
      ?? throw new ArgumentNullException(nameof(runFilenameTemplate));
    _timestampConfig = timestampConfig ?? throw new ArgumentNullException(nameof(timestampConfig));
    _direction = direction;
    _theme = theme ?? throw new ArgumentNullException(nameof(theme));
    _showFullDag = showFullDag;
    _perFlow = perFlow ?? throw new ArgumentNullException(nameof(perFlow));
    _logger = logger;
  }

  /// <inheritdoc/>
  public string ProviderId => "Flowthru.Mermaid";

  /// <summary>The output directory the provider exports files to.</summary>
  public string OutputDirectory => _outputDirectory;

  /// <inheritdoc/>
  public FlowIO<FlowUnit> Emit(FlowMetadataContext ctx) =>
    FlowIO.LiftAsync(async ct =>
    {
      var timestamp = _timestampConfig.GenerateTimestamp();
      var filename = FilenameTemplateParser.Render(
        ctx.EffectiveFlow.Label, _dagFilenameTemplate, timestamp
      ) + ".md";
      var filePath = Path.Combine(_outputDirectory, filename);

      _logger?.LogInformation("Exporting Mermaid DAG diagram to {FilePath}", filePath);

      Directory.CreateDirectory(_outputDirectory);
      var topology = _showFullDag ? ctx.MergedFlow : ctx.EffectiveFlow;
      var perFlow = ShouldEmitPerFlow(topology);
      var diagram = perFlow
        ? MermaidDiagramRenderer.RenderDagPerFlow(
            ctx, _showFullDag, _direction, _theme, _perFlow.HeadingLevel)
        : MermaidDiagramRenderer.RenderDag(ctx, _showFullDag, _direction, _theme);
      await AtomicWriteFile(filePath, diagram, ct).ConfigureAwait(false);

      _logger?.LogInformation(
        "Exported Mermaid DAG diagram ({Steps} steps, fullDag={FullDag}, perFlow={PerFlow})",
        topology.Steps.Count,
        _showFullDag,
        perFlow
      );

      return FlowUnit.Default;
    }, source: $"MermaidMetadataProvider.Emit[Dag,{ctx.EffectiveFlow.Label}]");

  /// <inheritdoc/>
  public FlowIO<FlowUnit> Emit(FlowRunMetadataContext ctx) =>
    FlowIO.LiftAsync(async ct =>
    {
      var timestamp = _timestampConfig.GenerateTimestamp();
      var filename = FilenameTemplateParser.Render(
        ctx.Static.EffectiveFlow.Label, _runFilenameTemplate, timestamp
      ) + ".md";
      var filePath = Path.Combine(_outputDirectory, filename);

      _logger?.LogInformation("Exporting Mermaid run diagram to {FilePath}", filePath);

      Directory.CreateDirectory(_outputDirectory);
      var topology = _showFullDag ? ctx.Static.MergedFlow : ctx.Static.EffectiveFlow;
      var perFlow = ShouldEmitPerFlow(topology);
      var diagram = perFlow
        ? MermaidDiagramRenderer.RenderRunPerFlow(
            ctx, _showFullDag, _direction, _theme, _perFlow.HeadingLevel)
        : MermaidDiagramRenderer.RenderRun(ctx, _showFullDag, _direction, _theme);
      await AtomicWriteFile(filePath, diagram, ct).ConfigureAwait(false);

      _logger?.LogInformation(
        "Exported Mermaid run diagram (success={Success}, steps={Steps}, fullDag={FullDag}, perFlow={PerFlow})",
        ctx.Result.IsSuccess,
        ctx.Result.StepResults.Count,
        _showFullDag,
        perFlow
      );

      return FlowUnit.Default;
    }, source: $"MermaidMetadataProvider.Emit[Run,{ctx.Static.EffectiveFlow.Label}]");

  /// <summary>
  /// Per-flow emission gate. <see cref="PerFlowMode.Auto"/> counts
  /// distinct <see cref="IStepNode.FlowLabel"/> values in the topology
  /// (the same grouping the renderer uses for subgraphs) and emits
  /// when that count meets the configured threshold. Counting off the
  /// rendered topology rather than the host's registered Flow list
  /// keeps the threshold consistent with the per-flow document's
  /// actual block count.
  /// </summary>
  private bool ShouldEmitPerFlow(BuiltFlow topology) => _perFlow.Mode switch
  {
    PerFlowMode.Disabled => false,
    PerFlowMode.Enabled => true,
    PerFlowMode.Auto => CountDistinctFlows(topology) >= _perFlow.AutoThreshold,
    _ => false,
  };

  private static int CountDistinctFlows(BuiltFlow topology) =>
    topology.Steps
      .Select(s => string.IsNullOrEmpty(s.FlowLabel) ? topology.Label : s.FlowLabel)
      .Distinct(StringComparer.Ordinal)
      .Count();

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
