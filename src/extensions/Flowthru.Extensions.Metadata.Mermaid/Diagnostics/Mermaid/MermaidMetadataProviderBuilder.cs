using Flowthru.Diagnostics.Mermaid.Internal;
using Microsoft.Extensions.Logging;

namespace Flowthru.Diagnostics.Mermaid;

/// <summary>
/// Fluent builder for <see cref="MermaidMetadataProvider"/>. Holds
/// configuration; constructs the provider on <see cref="Build"/>.
/// </summary>
public sealed class MermaidMetadataProviderBuilder
{
  private string _outputDirectory = "metadata";
  private string _dagFilenameTemplate = "dag-{FlowName}-{Timestamp}";
  private string _runFilenameTemplate = "run-{FlowName}-{Timestamp}";
  private TimestampConfiguration _timestampConfig = new();
  private MermaidFlowchartDirection _direction = MermaidFlowchartDirection.TopToBottom;
  private string _activeStepColor = "#2E7D32";
  private string _activeDataColor = "#2E7D32";
  private string _failedStepColor = "#C62828";
  private string _skippedStepColor = "#757575";
  private bool _showFullDag = true;
  private readonly PerFlowOptions _perFlow = new();
  private ILogger? _logger;

  /// <summary>Output directory for emitted Markdown files.</summary>
  public MermaidMetadataProviderBuilder WithOutputDirectory(string directory)
  {
    _outputDirectory = directory ?? throw new ArgumentNullException(nameof(directory));
    return this;
  }

  /// <summary>Filename template for the pre-run DAG diagram. Tokens: <c>{FlowName}</c>, <c>{Timestamp}</c>.</summary>
  public MermaidMetadataProviderBuilder WithFilenameTemplate(string template)
  {
    _dagFilenameTemplate = template ?? throw new ArgumentNullException(nameof(template));
    return this;
  }

  /// <summary>Filename template for the post-run result diagram.</summary>
  public MermaidMetadataProviderBuilder WithRunFilenameTemplate(string template)
  {
    _runFilenameTemplate = template ?? throw new ArgumentNullException(nameof(template));
    return this;
  }

  /// <summary>
  /// Enable timestamps in filenames; pass <c>null</c> for default
  /// format (<c>yyyy-MM-dd-HH-mm-ss</c>) or a custom DateTime format.
  /// </summary>
  public MermaidMetadataProviderBuilder WithTimestamp(string? format = null)
  {
    _timestampConfig = format is null
      ? new TimestampConfiguration { IncludeTimestamp = true }
      : new TimestampConfiguration { IncludeTimestamp = true, Format = format };
    return this;
  }

  /// <summary>Layout direction for the flowchart. Defaults to top-to-bottom.</summary>
  public MermaidMetadataProviderBuilder WithDirection(MermaidFlowchartDirection direction)
  {
    _direction = direction;
    return this;
  }

  /// <summary>Hex colour for active / succeeded step nodes (post-run).</summary>
  public MermaidMetadataProviderBuilder WithActiveStepColor(string color)
  {
    _activeStepColor = color ?? throw new ArgumentNullException(nameof(color));
    return this;
  }

  /// <summary>Hex colour for active catalog-item nodes.</summary>
  public MermaidMetadataProviderBuilder WithActiveDataColor(string color)
  {
    _activeDataColor = color ?? throw new ArgumentNullException(nameof(color));
    return this;
  }

  /// <summary>Hex colour for failed step nodes (post-run).</summary>
  public MermaidMetadataProviderBuilder WithFailedStepColor(string color)
  {
    _failedStepColor = color ?? throw new ArgumentNullException(nameof(color));
    return this;
  }

  /// <summary>Hex colour for skipped step nodes (post-run).</summary>
  public MermaidMetadataProviderBuilder WithSkippedStepColor(string color)
  {
    _skippedStepColor = color ?? throw new ArgumentNullException(nameof(color));
    return this;
  }

  /// <summary>
  /// Slice-aware rendering toggle. When <c>true</c> (default), the
  /// renderer emits the full merged DAG and styles inactive nodes
  /// (steps and items not in the active slice) with a muted theme so
  /// readers can see the surrounding topology. When <c>false</c>, the
  /// renderer filters inactive nodes out entirely and emits only the
  /// active subset. Has no visible effect when the host runs the
  /// merged DAG without slicing — every node is active either way.
  /// </summary>
  public MermaidMetadataProviderBuilder WithShowFullDag(bool showFullDag)
  {
    _showFullDag = showFullDag;
    return this;
  }

  /// <summary>
  /// Configure per-flow rendering — whether the merged DAG file holds
  /// a per-flow rendering instead of the monolithic single-block view,
  /// the auto-mode threshold, and the Markdown heading level used to
  /// label each per-flow block. See <see cref="PerFlowOptions"/> for
  /// each knob's default. Output filenames are unaffected by per-flow
  /// rendering — downstream readers always open the same file.
  /// </summary>
  public MermaidMetadataProviderBuilder WithPerFlow(Action<PerFlowOptions> configure)
  {
    if (configure is null) throw new ArgumentNullException(nameof(configure));
    configure(_perFlow);
    return this;
  }

  /// <summary>Optional logger for export targets and outcomes.</summary>
  public MermaidMetadataProviderBuilder WithLogger(ILogger logger)
  {
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    return this;
  }

  /// <summary>Materialise the provider. Validates the timestamp configuration.</summary>
  public MermaidMetadataProvider Build()
  {
    _timestampConfig.Validate();
    var theme = new MermaidDiagramRenderer.Theme(
      ActiveStepColor: _activeStepColor,
      ActiveDataColor: _activeDataColor,
      FailedStepColor: _failedStepColor,
      SkippedStepColor: _skippedStepColor
    );
    return new MermaidMetadataProvider(
      _outputDirectory,
      _dagFilenameTemplate,
      _runFilenameTemplate,
      _timestampConfig,
      _direction,
      theme,
      _showFullDag,
      _perFlow,
      _logger
    );
  }
}
