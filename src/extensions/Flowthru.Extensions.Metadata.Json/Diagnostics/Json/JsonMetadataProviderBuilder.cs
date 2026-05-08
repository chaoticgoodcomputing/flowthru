using Flowthru.Diagnostics.Json.Internal;
using Microsoft.Extensions.Logging;

namespace Flowthru.Diagnostics.Json;

/// <summary>
/// Fluent builder for <see cref="JsonMetadataProvider"/>. Holds
/// configuration; constructs the provider on
/// <see cref="Build"/>.
/// </summary>
public sealed class JsonMetadataProviderBuilder
{
  private string _outputDirectory = "metadata";
  private string _dagFilenameTemplate = "dag-{FlowName}-{Timestamp}";
  private string _runFilenameTemplate = "run-{FlowName}-{Timestamp}";
  private TimestampConfiguration _timestampConfig = new();
  private bool _useCompactFormat = false;
  private ILogger? _logger;

  /// <summary>Output directory for emitted JSON files. Created on first export if absent.</summary>
  public JsonMetadataProviderBuilder WithOutputDirectory(string directory)
  {
    _outputDirectory = directory ?? throw new ArgumentNullException(nameof(directory));
    return this;
  }

  /// <summary>
  /// Filename template for the pre-run DAG manifest. Tokens:
  /// <c>{FlowName}</c>, <c>{Timestamp}</c>.
  /// </summary>
  public JsonMetadataProviderBuilder WithFilenameTemplate(string template)
  {
    _dagFilenameTemplate = template ?? throw new ArgumentNullException(nameof(template));
    return this;
  }

  /// <summary>
  /// Filename template for the post-run result file. Same tokens as
  /// <see cref="WithFilenameTemplate"/>.
  /// </summary>
  public JsonMetadataProviderBuilder WithRunFilenameTemplate(string template)
  {
    _runFilenameTemplate = template ?? throw new ArgumentNullException(nameof(template));
    return this;
  }

  /// <summary>
  /// Enable timestamps in filenames; pass <c>null</c> for default
  /// format (<c>yyyy-MM-dd-HH-mm-ss</c>) or a custom DateTime format.
  /// </summary>
  public JsonMetadataProviderBuilder WithTimestamp(string? format = null)
  {
    _timestampConfig = format is null
      ? new TimestampConfiguration { IncludeTimestamp = true }
      : new TimestampConfiguration { IncludeTimestamp = true, Format = format };
    return this;
  }

  /// <summary>Compact JSON output (no indentation).</summary>
  public JsonMetadataProviderBuilder UseCompactFormat()
  {
    _useCompactFormat = true;
    return this;
  }

  /// <summary>Indented JSON output (default).</summary>
  public JsonMetadataProviderBuilder UseIndentedFormat()
  {
    _useCompactFormat = false;
    return this;
  }

  /// <summary>Optional logger — when set, the provider logs export targets and outcomes.</summary>
  public JsonMetadataProviderBuilder WithLogger(ILogger logger)
  {
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    return this;
  }

  /// <summary>Materialise the provider. Validates the timestamp configuration; throws on bad format.</summary>
  public JsonMetadataProvider Build()
  {
    _timestampConfig.Validate();
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
