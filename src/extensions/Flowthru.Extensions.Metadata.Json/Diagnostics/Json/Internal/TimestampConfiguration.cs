namespace Flowthru.Diagnostics.Json.Internal;

/// <summary>
/// Configuration for timestamp handling in metadata file exports.
/// Carry-over helper local to the JSON metadata extension; lives in
/// <c>Internal</c> because no end-user names it directly.
/// </summary>
/// <remarks>
/// When timestamps are disabled, subsequent exports overwrite previous
/// files with the same flow name — useful for human inspection but
/// destructive if you want a history. Default is <em>disabled</em>;
/// catalog authors opt in via the builder's
/// <c>WithTimestamp(...)</c> setter.
/// </remarks>
internal sealed class TimestampConfiguration
{
  /// <summary>Include timestamps in the rendered filename.</summary>
  public bool IncludeTimestamp { get; set; } = false;

  /// <summary>Timestamp format string. Must be a valid <see cref="DateTime.ToString(string)"/> format.</summary>
  public string Format { get; set; } = "yyyy-MM-dd-HH-mm-ss";

  /// <summary>
  /// Validate the configuration. Builders should call this from their
  /// <c>Build()</c> implementation so misconfigurations fail fast at
  /// registration time.
  /// </summary>
  public void Validate()
  {
    if (IncludeTimestamp && string.IsNullOrWhiteSpace(Format))
    {
      throw new ArgumentException(
        "Timestamp format cannot be null or empty when IncludeTimestamp is true.",
        nameof(Format)
      );
    }

    if (IncludeTimestamp)
    {
      try
      {
        _ = DateTime.Now.ToString(Format);
      }
      catch (FormatException ex)
      {
        throw new ArgumentException(
          $"Invalid timestamp format string: '{Format}'.",
          nameof(Format),
          ex
        );
      }
    }
  }

  /// <summary>Produce a timestamp string per the current configuration, or <c>null</c> when disabled.</summary>
  public string? GenerateTimestamp() =>
    IncludeTimestamp ? DateTime.Now.ToString(Format) : null;
}
