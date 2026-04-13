using Flowthru.Meta.Providers;
using Microsoft.Extensions.Logging;

namespace Flowthru.Meta;

/// <summary>
/// Builder for configuring <see cref="MermaidMetadataProvider"/> options.
/// </summary>
public class MermaidMetadataProviderBuilder
{
  private string _outputDirectory = "metadata";
  private string _dagFilenameTemplate = "dag-{FlowName}-{Timestamp}-{SliceType}";
  private string _runFilenameTemplate = "run-{FlowName}-{Timestamp}-{SliceType}";
  private Core.Meta.TimestampConfiguration _timestampConfig = new();
  private MermaidMetadataProvider.MermaidFlowchartDirection _direction = MermaidMetadataProvider
    .MermaidFlowchartDirection
    .TopToBottom;
  private string _activeStepColor = "#2E7D32";
  private string _activeDataColor = "#2E7D32";
  private string _failedStepColor = "#C62828";
  private string _notRunStepColor = "#757575";
  private ILogger? _logger;

  /// <summary>
  /// Sets the output directory for metadata files.
  /// </summary>
  public MermaidMetadataProviderBuilder WithOutputDirectory(string directory)
  {
    _outputDirectory = directory ?? throw new ArgumentNullException(nameof(directory));
    return this;
  }

  /// <summary>
  /// Sets the filename template for pre-run DAG diagram files.
  /// </summary>
  /// <param name="template">Template with placeholders: {FlowName}, {Timestamp}, {SliceType}</param>
  public MermaidMetadataProviderBuilder WithFilenameTemplate(string template)
  {
    _dagFilenameTemplate = template ?? throw new ArgumentNullException(nameof(template));
    return this;
  }

  /// <summary>
  /// Sets the filename template for post-run result diagram files.
  /// </summary>
  /// <param name="template">Template with placeholders: {FlowName}, {Timestamp}, {SliceType}</param>
  public MermaidMetadataProviderBuilder WithRunFilenameTemplate(string template)
  {
    _runFilenameTemplate = template ?? throw new ArgumentNullException(nameof(template));
    return this;
  }

  /// <summary>
  /// Sets the timestamp format for filename generation.
  /// </summary>
  /// <param name="format">Timestamp format string (e.g., "yyyy-MM-dd_HH-mm-ss")</param>
  public MermaidMetadataProviderBuilder WithTimestamp(string? format = null)
  {
    _timestampConfig =
      format == null ? new() : new Core.Meta.TimestampConfiguration { Format = format };
    return this;
  }

  /// <summary>
  /// Sets the flowchart direction.
  /// </summary>
  public MermaidMetadataProviderBuilder WithDirection(
    MermaidMetadataProvider.MermaidFlowchartDirection direction
  )
  {
    _direction = direction;
    return this;
  }

  /// <summary>
  /// Sets the color for active (sliced) step nodes in the pre-run DAG diagram.
  /// </summary>
  /// <param name="color">Hex color code (e.g., "#2E7D32")</param>
  public MermaidMetadataProviderBuilder WithActiveStepColor(string color)
  {
    _activeStepColor = color ?? throw new ArgumentNullException(nameof(color));
    return this;
  }

  /// <summary>
  /// Sets the color for active (sliced) catalog entries in the pre-run DAG diagram.
  /// </summary>
  /// <param name="color">Hex color code (e.g., "#2E7D32")</param>
  public MermaidMetadataProviderBuilder WithActiveDataColor(string color)
  {
    _activeDataColor = color ?? throw new ArgumentNullException(nameof(color));
    return this;
  }

  /// <summary>
  /// Sets the color for failed step nodes in the post-run diagram.
  /// </summary>
  /// <param name="color">Hex color code (e.g., "#C62828")</param>
  public MermaidMetadataProviderBuilder WithFailedStepColor(string color)
  {
    _failedStepColor = color ?? throw new ArgumentNullException(nameof(color));
    return this;
  }

  /// <summary>
  /// Sets the color for steps that did not run in the post-run diagram.
  /// </summary>
  /// <param name="color">Hex color code (e.g., "#757575")</param>
  public MermaidMetadataProviderBuilder WithNotRunStepColor(string color)
  {
    _notRunStepColor = color ?? throw new ArgumentNullException(nameof(color));
    return this;
  }

  /// <summary>
  /// Sets a custom logger for this provider.
  /// </summary>
  public MermaidMetadataProviderBuilder WithLogger(ILogger logger)
  {
    _logger = logger;
    return this;
  }

  /// <summary>
  /// Builds the Mermaid metadata provider with the configured options.
  /// </summary>
  public MermaidMetadataProvider Build()
  {
    return new MermaidMetadataProvider(
      _outputDirectory,
      _dagFilenameTemplate,
      _runFilenameTemplate,
      _timestampConfig,
      _direction,
      _activeStepColor,
      _activeDataColor,
      _failedStepColor,
      _notRunStepColor,
      _logger
    );
  }
}
