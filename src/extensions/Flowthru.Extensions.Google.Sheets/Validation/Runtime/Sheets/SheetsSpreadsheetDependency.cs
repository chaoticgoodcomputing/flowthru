namespace Flowthru.Validation.Runtime.Sheets;

/// <summary>
/// Conflict identity of the Google spreadsheet a Sheets catalog item
/// reads from or writes to (ADR-0019). A spreadsheet is shared mutable
/// state behind a per-user API quota: concurrent writes race (overlapping
/// <c>batchUpdate</c> ranges, last-write-wins) and pile onto the quota.
/// Surfaced through Core's <see cref="ServiceDependency.External"/> so the
/// scheduler serializes writes to one spreadsheet (write capacity 1) while
/// letting reads parallelize (read capacity ∞).
/// </summary>
/// <remarks>
/// <para>
/// Keyed on the spreadsheet id, not the <c>(spreadsheetId, tableName)</c>
/// pair: the Sheets <c>batchUpdate</c> write path and the quota are
/// per-spreadsheet, so concurrent writes to different tabs of the same
/// spreadsheet still contend. The adapter constructs this; capacities ride
/// on the dependency because the resolving contributor sees only the
/// dependency, not the originating item.
/// </para>
/// </remarks>
internal sealed record SheetsSpreadsheetDependency(
  string SpreadsheetId,
  int WriteCapacity,
  int ReadCapacity
) : IExtensionServiceDependency, ICapacityConstrainable
{
  /// <inheritdoc/>
  public string DagId => $"sheets:{SpreadsheetId}";

  /// <inheritdoc/>
  public string DisplayName => $"sheet:{SpreadsheetId}";

  /// <inheritdoc/>
  public string Category => "sheets";

  /// <inheritdoc/>
  public IExtensionServiceDependency ClampTo(int writeCapacity, int readCapacity) =>
    this with
    {
      WriteCapacity = Math.Min(WriteCapacity, writeCapacity),
      ReadCapacity = Math.Min(ReadCapacity, readCapacity),
    };
}
