using Flowthru.Meta.Providers;
using Microsoft.Extensions.Logging;

namespace Flowthru.Meta;

/// <summary>
/// Builder for configuring Mermaid diagram provider options.
/// </summary>
public class MermaidMetadataProviderBuilder
{
  private string _outputDirectory = "metadata";
  private string _filenameTemplate = "dag-{FlowName}-{Timestamp}-{SliceType}";
  private TimestampConfiguration _timestampConfig = new();
  private MermaidMetadataProvider.MermaidFlowchartDirection _direction = MermaidMetadataProvider
    .MermaidFlowchartDirection
    .TopToBottom;
  private string _activeNodeColor = "#2E7D32";
  private string _activeDataColor = "#2E7D32";
  private ILogger? _logger;

  /// <summary>
  /// Sets the output directory for metadata files.
  /// </summary>
  /// <param name="directory">Directory path (relative or absolute)</param>
  /// <returns>This builder for fluent chaining</returns>
  public MermaidMetadataProviderBuilder WithOutputDirectory(string directory)
  {
    _outputDirectory = directory ?? throw new ArgumentNullException(nameof(directory));
    return this;
  }

  /// <summary>
  /// Sets the filename template for metadata files.
  /// </summary>
  /// <param name="template">Template with placeholders: {FlowName}, {Timestamp}, {SliceType}</param>
  /// <returns>This builder for fluent chaining</returns>
  public MermaidMetadataProviderBuilder WithFilenameTemplate(string template)
  {
    _filenameTemplate = template ?? throw new ArgumentNullException(nameof(template));
    return this;
  }

  /// <summary>
  /// Sets the timestamp format for filename generation.
  /// </summary>
  /// <param name="format">Timestamp format string (e.g., "yyyy-MM-dd_HH-mm-ss")</param>
  /// <returns>This builder for fluent chaining</returns>
  public MermaidMetadataProviderBuilder WithTimestamp(string? format = null)
  {
    _timestampConfig = format == null ? new() : new() { Format = format };
    return this;
  }

  /// <summary>
  /// Sets the flowchart direction.
  /// </summary>
  /// <param name="direction">Direction for the flowchart (TB, LR, BT, RL)</param>
  /// <returns>This builder for fluent chaining</returns>
  public MermaidMetadataProviderBuilder WithDirection(
    MermaidMetadataProvider.MermaidFlowchartDirection direction
  )
  {
    _direction = direction;
    return this;
  }

  /// <summary>
  /// Sets the color for active (sliced) nodes.
  /// </summary>
  /// <param name="color">Hex color code (e.g., "#2E7D32")</param>
  /// <returns>This builder for fluent chaining</returns>
  public MermaidMetadataProviderBuilder WithActiveStepColor(string color)
  {
    _activeNodeColor = color ?? throw new ArgumentNullException(nameof(color));
    return this;
  }

  /// <summary>
  /// Sets the color for active (sliced) catalog entries.
  /// </summary>
  /// <param name="color">Hex color code (e.g., "#2E7D32")</param>
  /// <returns>This builder for fluent chaining</returns>
  public MermaidMetadataProviderBuilder WithActiveDataColor(string color)
  {
    _activeDataColor = color ?? throw new ArgumentNullException(nameof(color));
    return this;
  }

  /// <summary>
  /// Sets a custom logger for this provider.
  /// </summary>
  /// <param name="logger">Logger instance</param>
  /// <returns>This builder for fluent chaining</returns>
  public MermaidMetadataProviderBuilder WithLogger(ILogger logger)
  {
    _logger = logger;
    return this;
  }

  /// <summary>
  /// Builds the Mermaid metadata provider with the configured options.
  /// </summary>
  /// <returns>A configured <see cref="MermaidMetadataProvider"/> instance</returns>
  public MermaidMetadataProvider Build()
  {
    return new MermaidMetadataProvider(
      _outputDirectory,
      _filenameTemplate,
      _timestampConfig,
      _direction,
      _activeNodeColor,
      _activeDataColor,
      _logger
    );
  }
}
