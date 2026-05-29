namespace Flowthru.Data.Storage.Sheets;

/// <summary>
/// The spreadsheet a catalog item addresses could not be reached — it does not
/// exist, or the gateway's principal lacks access to it. Distinct from a table
/// being absent within a reachable spreadsheet: a missing table is a
/// <see langword="null"/> from <see cref="ISheetsGateway.ResolveTable"/>
/// (create-if-absent can recover); a missing or inaccessible <em>spreadsheet</em>
/// is a hard failure (Flowthru creates tables, not spreadsheets).
/// </summary>
/// <remarks>
/// <para>
/// This is the neutral, gateway-level shape of Google's spreadsheet-level
/// <c>404</c> (not-found) and <c>403</c> (access-denied). It lives on the
/// gateway namespace (not in any one gateway) because <strong>both</strong>
/// gateways surface it: the production <c>SheetsServiceGateway</c> maps the
/// Google HTTP status onto it, and the offline <c>InMemorySheetsGateway</c>
/// throws it when an operation targets a spreadsheet that was never registered.
/// </para>
/// <para>
/// <strong>Permanent by contract.</strong> Unlike
/// <see cref="SheetsRateLimitException"/>, this is not retryable — re-issuing the
/// same request against the same (missing/forbidden) spreadsheet fails the same
/// way. Pre-flight maps it to a <c>ValidationErrorType</c> so the failure is
/// caught before the run, not at first Load/Save.
/// </para>
/// </remarks>
public sealed class SheetsSpreadsheetAccessException : Exception
{
  /// <summary>The spreadsheet id the failing operation addressed.</summary>
  public string SpreadsheetId { get; }

  /// <summary>Why the spreadsheet was unreachable, when the gateway can tell.</summary>
  public SheetsSpreadsheetAccessFailure Failure { get; }

  /// <summary>Build a spreadsheet-access failure for <paramref name="spreadsheetId"/>.</summary>
  public SheetsSpreadsheetAccessException(
    string spreadsheetId,
    SheetsSpreadsheetAccessFailure failure,
    string message)
    : base(message)
  {
    SpreadsheetId = spreadsheetId ?? throw new ArgumentNullException(nameof(spreadsheetId));
    Failure = failure;
  }

  /// <summary>Build a spreadsheet-access failure wrapping an underlying cause.</summary>
  public SheetsSpreadsheetAccessException(
    string spreadsheetId,
    SheetsSpreadsheetAccessFailure failure,
    string message,
    Exception innerException)
    : base(message, innerException)
  {
    SpreadsheetId = spreadsheetId ?? throw new ArgumentNullException(nameof(spreadsheetId));
    Failure = failure;
  }
}

/// <summary>Why a spreadsheet-level operation failed to reach its target.</summary>
public enum SheetsSpreadsheetAccessFailure
{
  /// <summary>The spreadsheet does not exist (Google <c>404</c>).</summary>
  NotFound = 0,

  /// <summary>The principal is not permitted to access the spreadsheet (Google <c>403</c>).</summary>
  AccessDenied = 1,

  /// <summary>Unreachable for a reason the gateway could not classify.</summary>
  Unknown = 2,
}
