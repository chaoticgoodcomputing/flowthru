namespace Flowthru.Diagnostics.Mermaid.Internal;

/// <summary>
/// Configuration for timestamp handling in metadata file exports.
/// Carry-over helper local to the Mermaid metadata extension.
/// </summary>
/// <remarks>
/// Duplicated verbatim with the JSON metadata extension —
/// deduplication is a tracked carryover (lift into a shared
/// Diagnostics-helper namespace once a third metadata extension
/// arrives).
/// </remarks>
internal sealed class TimestampConfiguration
{
  /// <summary>Include timestamps in the rendered filename.</summary>
  public bool IncludeTimestamp { get; set; } = false;

  /// <summary>Timestamp format string. Must be a valid <see cref="DateTime.ToString(string)"/> format.</summary>
  public string Format { get; set; } = "yyyy-MM-dd-HH-mm-ss";

  /// <summary>Validate the configuration; throws if format is malformed.</summary>
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
