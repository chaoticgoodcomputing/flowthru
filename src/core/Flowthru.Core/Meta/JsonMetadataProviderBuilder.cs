using Flowthru.Meta.Providers;
using Microsoft.Extensions.Logging;

namespace Flowthru.Meta;

/// <summary>
/// Builder for configuring JSON metadata provider options.
/// </summary>
public class JsonMetadataProviderBuilder
{
  private string _outputDirectory = "metadata";
  private string _filenameTemplate = "dag-{FlowName}-{Timestamp}-{SliceType}";
  private TimestampConfiguration _timestampConfig = new();
  private bool _useCompactFormat = false;
  private ILogger? _logger;

  /// <summary>
  /// Sets the output directory for metadata files.
  /// </summary>
  /// <param name="directory">Directory path (relative or absolute)</param>
  /// <returns>This builder for fluent chaining</returns>
  public JsonMetadataProviderBuilder WithOutputDirectory(string directory)
  {
    _outputDirectory = directory ?? throw new ArgumentNullException(nameof(directory));
    return this;
  }

  /// <summary>
  /// Sets the filename template for metadata files.
  /// </summary>
  /// <param name="template">Template with placeholders: {FlowName}, {Timestamp}, {SliceType}</param>
  /// <returns>This builder for fluent chaining</returns>
  public JsonMetadataProviderBuilder WithFilenameTemplate(string template)
  {
    _filenameTemplate = template ?? throw new ArgumentNullException(nameof(template));
    return this;
  }

  /// <summary>
  /// Sets the timestamp format for filename generation.
  /// </summary>
  /// <param name="format">Timestamp format string (e.g., "yyyy-MM-dd_HH-mm-ss")</param>
  /// <returns>This builder for fluent chaining</returns>
  public JsonMetadataProviderBuilder WithTimestamp(string? format = null)
  {
    _timestampConfig = format == null ? new() : new() { Format = format };
    return this;
  }

  /// <summary>
  /// Enables compact JSON format (no indentation).
  /// </summary>
  /// <returns>This builder for fluent chaining</returns>
  public JsonMetadataProviderBuilder UseCompactFormat()
  {
    _useCompactFormat = true;
    return this;
  }

  /// <summary>
  /// Enables indented JSON format (default).
  /// </summary>
  /// <returns>This builder for fluent chaining</returns>
  public JsonMetadataProviderBuilder UseIndentedFormat()
  {
    _useCompactFormat = false;
    return this;
  }

  /// <summary>
  /// Sets a custom logger for this provider.
  /// </summary>
  /// <param name="logger">Logger instance</param>
  /// <returns>This builder for fluent chaining</returns>
  public JsonMetadataProviderBuilder WithLogger(ILogger logger)
  {
    _logger = logger;
    return this;
  }

  /// <summary>
  /// Builds the JSON metadata provider with the configured options.
  /// </summary>
  /// <returns>A configured <see cref="JsonMetadataProvider"/> instance</returns>
  public JsonMetadataProvider Build()
  {
    return new JsonMetadataProvider(
      _outputDirectory,
      _filenameTemplate,
      _timestampConfig,
      _useCompactFormat,
      _logger
    );
  }
}
