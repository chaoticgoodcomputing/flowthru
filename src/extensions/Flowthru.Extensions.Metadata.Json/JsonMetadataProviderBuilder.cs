using Flowthru.Meta.Providers;
using Microsoft.Extensions.Logging;

namespace Flowthru.Meta;

/// <summary>
/// Builder for configuring <see cref="JsonMetadataProvider"/> options.
/// </summary>
public class JsonMetadataProviderBuilder
{
  private string _outputDirectory = "metadata";
  private string _dagFilenameTemplate = "dag-{FlowName}-{Timestamp}-{SliceType}";
  private string _runFilenameTemplate = "run-{FlowName}-{Timestamp}-{SliceType}";
  private Core.Meta.TimestampConfiguration _timestampConfig = new();
  private bool _useCompactFormat = false;
  private ILogger? _logger;

  /// <summary>
  /// Sets the output directory for metadata files.
  /// </summary>
  public JsonMetadataProviderBuilder WithOutputDirectory(string directory)
  {
    _outputDirectory = directory ?? throw new ArgumentNullException(nameof(directory));
    return this;
  }

  /// <summary>
  /// Sets the filename template for pre-run DAG export files.
  /// </summary>
  /// <param name="template">Template with placeholders: {FlowName}, {Timestamp}, {SliceType}</param>
  public JsonMetadataProviderBuilder WithFilenameTemplate(string template)
  {
    _dagFilenameTemplate = template ?? throw new ArgumentNullException(nameof(template));
    return this;
  }

  /// <summary>
  /// Sets the filename template for post-run result export files.
  /// </summary>
  /// <param name="template">Template with placeholders: {FlowName}, {Timestamp}, {SliceType}</param>
  public JsonMetadataProviderBuilder WithRunFilenameTemplate(string template)
  {
    _runFilenameTemplate = template ?? throw new ArgumentNullException(nameof(template));
    return this;
  }

  /// <summary>
  /// Sets the timestamp format for filename generation.
  /// </summary>
  /// <param name="format">Timestamp format string (e.g., "yyyy-MM-dd_HH-mm-ss")</param>
  public JsonMetadataProviderBuilder WithTimestamp(string? format = null)
  {
    _timestampConfig =
      format == null ? new() : new Core.Meta.TimestampConfiguration { Format = format };
    return this;
  }

  /// <summary>
  /// Enables compact JSON format (no indentation).
  /// </summary>
  public JsonMetadataProviderBuilder UseCompactFormat()
  {
    _useCompactFormat = true;
    return this;
  }

  /// <summary>
  /// Enables indented JSON format (default).
  /// </summary>
  public JsonMetadataProviderBuilder UseIndentedFormat()
  {
    _useCompactFormat = false;
    return this;
  }

  /// <summary>
  /// Sets a custom logger for this provider.
  /// </summary>
  public JsonMetadataProviderBuilder WithLogger(ILogger logger)
  {
    _logger = logger;
    return this;
  }

  /// <summary>
  /// Builds the JSON metadata provider with the configured options.
  /// </summary>
  public JsonMetadataProvider Build()
  {
    return new JsonMetadataProvider(
      _outputDirectory,
      _dagFilenameTemplate,
      _runFilenameTemplate,
      _timestampConfig,
      _useCompactFormat,
      _logger
    );
  }
}
