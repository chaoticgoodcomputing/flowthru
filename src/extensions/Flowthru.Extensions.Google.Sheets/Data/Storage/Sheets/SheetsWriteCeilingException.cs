namespace Flowthru.Data.Storage.Sheets;

/// <summary>
/// A single write exceeded Google Sheets' single-batch ceiling — either the
/// payload was rejected as too large (HTTP <c>413</c>) or the batch update
/// exceeded the server-side processing-timeout (~180 s). Output size is unknowable
/// at pre-flight (it is produced by upstream Steps), so this is a <em>runtime</em>
/// failure surfaced when the over-ceiling write is actually issued.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Not transient.</strong> This deliberately does <strong>not</strong>
/// derive from <see cref="SheetsRateLimitException"/>, so
/// <see cref="RetryingSheetsGateway"/> will not retry it — re-issuing the same
/// over-ceiling payload fails the same way. Chunked / multi-batch writes are out
/// of scope for v1 (they would break the write's atomicity), so the failure is
/// loud and actionable rather than silently truncating.
/// </para>
/// <para>
/// Carries the <c>FTGS1608</c> provenance code so the failure is grep-able in run
/// output, alongside which ceiling was breached.
/// </para>
/// </remarks>
public sealed class SheetsWriteCeilingException : Exception
{
  /// <summary>The provenance code embedded in the message for grep-ability.</summary>
  public const string Code = "FTGS1608";

  /// <summary>The spreadsheet the over-ceiling write addressed.</summary>
  public string SpreadsheetId { get; }

  /// <summary>Which single-batch ceiling the write breached.</summary>
  public SheetsWriteCeiling Ceiling { get; }

  /// <summary>
  /// Build a write-ceiling failure for <paramref name="spreadsheetId"/>, naming
  /// the breached <paramref name="ceiling"/> and chaining the underlying cause.
  /// </summary>
  public SheetsWriteCeilingException(
    string spreadsheetId,
    SheetsWriteCeiling ceiling,
    Exception innerException)
    : base(BuildMessage(spreadsheetId, ceiling), innerException)
  {
    SpreadsheetId = spreadsheetId ?? throw new ArgumentNullException(nameof(spreadsheetId));
    Ceiling = ceiling;
  }

  private static string BuildMessage(string spreadsheetId, SheetsWriteCeiling ceiling)
  {
    var detail = ceiling switch
    {
      SheetsWriteCeiling.PayloadTooLarge =>
        "the write payload exceeds Sheets' single-batch size ceiling (~2 MB recommended)",
      SheetsWriteCeiling.ProcessingTimeout =>
        "the write exceeded Sheets' single-batch processing timeout (~180 s)",
      _ => "the write exceeds Sheets' single-batch ceiling",
    };

    return $"[{Code}] Writing to spreadsheet '{spreadsheetId}' failed: {detail}. "
      + "Flowthru replaces a table's rows in one atomic batch; chunked writes are "
      + "not supported in v1. Reduce the row/cell count of this write (e.g. filter "
      + "or aggregate upstream) so it fits a single batch.";
  }
}

/// <summary>Which single-batch ceiling a Sheets write breached.</summary>
public enum SheetsWriteCeiling
{
  /// <summary>The payload was rejected as too large (HTTP <c>413</c>).</summary>
  PayloadTooLarge = 0,

  /// <summary>The batch update exceeded the server-side processing timeout (~180 s).</summary>
  ProcessingTimeout = 1,
}
