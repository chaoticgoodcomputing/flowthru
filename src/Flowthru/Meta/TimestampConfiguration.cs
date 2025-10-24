namespace Flowthru.Meta;

/// <summary>
/// Configuration for timestamp handling in metadata file exports.
/// </summary>
/// <remarks>
/// <para>
/// Controls whether and how timestamps are included in metadata filenames.
/// This configuration applies to all metadata providers (JSON, Mermaid, etc.)
/// to ensure consistent filename generation.
/// </para>
/// <para>
/// <strong>Default behavior:</strong> Timestamps are included with format "yyyyMMdd-HHmmss"
/// </para>
/// <para>
/// <strong>Example filenames:</strong>
/// </para>
/// <list type="bullet">
/// <item>With timestamp: <c>dag-DataProcessing-20251024-143052.json</c></item>
/// <item>Without timestamp: <c>dag-DataProcessing.json</c></item>
/// </list>
/// <para>
/// <strong>Warning:</strong> When timestamps are disabled, subsequent exports will overwrite
/// previous files with the same pipeline name.
/// </para>
/// </remarks>
public class TimestampConfiguration {
  /// <summary>
  /// Gets or sets whether to include timestamps in metadata filenames.
  /// </summary>
  /// <remarks>
  /// Default: true
  /// When false, files will be named without timestamps and will overwrite on each export.
  /// </remarks>
  public bool IncludeTimestamp { get; set; } = true;

  /// <summary>
  /// Gets or sets the timestamp format string.
  /// </summary>
  /// <remarks>
  /// Default: "yyyyMMdd-HHmmss" (e.g., "20251024-143052")
  /// Must be a valid DateTime format string compatible with DateTime.ToString().
  /// Only used when IncludeTimestamp is true.
  /// </remarks>
  public string Format { get; set; } = "yyyyMMdd-HHmmss";

  /// <summary>
  /// Validates the timestamp configuration.
  /// </summary>
  /// <exception cref="ArgumentException">Thrown if format string is invalid</exception>
  internal void Validate() {
    if (IncludeTimestamp && string.IsNullOrWhiteSpace(Format)) {
      throw new ArgumentException("Timestamp format cannot be null or empty when IncludeTimestamp is true", nameof(Format));
    }

    // Validate format string by attempting to format current time
    if (IncludeTimestamp) {
      try {
        _ = DateTime.Now.ToString(Format);
      } catch (FormatException ex) {
        throw new ArgumentException($"Invalid timestamp format string: '{Format}'", nameof(Format), ex);
      }
    }
  }

  /// <summary>
  /// Generates a timestamp string based on current configuration.
  /// </summary>
  /// <returns>Formatted timestamp string, or null if timestamps are disabled</returns>
  internal string? GenerateTimestamp() {
    return IncludeTimestamp ? DateTime.Now.ToString(Format) : null;
  }
}
