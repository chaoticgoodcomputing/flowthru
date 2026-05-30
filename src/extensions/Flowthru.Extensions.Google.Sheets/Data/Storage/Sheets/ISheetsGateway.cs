namespace Flowthru.Data.Storage.Sheets;

/// <summary>
/// The narrow seam the Sheets catalog adapter calls — the only operations a
/// Sheets read, write, create, or pre-flight check needs. Speaks the
/// Flowthru-neutral tabular vocabulary (<see cref="TableSchema"/>,
/// <see cref="TableData"/>, <see cref="FieldValue"/>, <see cref="ResolvedTable"/>);
/// no Google SDK type appears on this surface, so an offline in-memory
/// implementation can satisfy it without referencing
/// <c>Google.Apis.Sheets.v4</c>.
/// </summary>
/// <remarks>
/// <para>
/// A catalog item is addressed by <c>(spreadsheetId, table name)</c> — a stable
/// table name, not a fragile A1 range. The table tracks its own extent, so the
/// gateway resolves a name to the table's schema and range; range drift is the
/// table's problem, not Flowthru's.
/// </para>
/// <para>
/// Methods are asynchronous and may throw on API failure — the adapter lifts
/// the call into <c>FlowIO</c> and is responsible for retry/backoff. The seam
/// itself stays minimal and exception-based.
/// </para>
/// <para>
/// A gateway instance is bound to one set of credentials (one catalog).
/// Crossing Google accounts is two catalogs with two gateways.
/// </para>
/// </remarks>
public interface ISheetsGateway
{
  /// <summary>
  /// Resolve the table named <paramref name="tableName"/> in
  /// <paramref name="spreadsheetId"/> to its live schema (column names + types)
  /// and grid range. Returns <see langword="null"/> <strong>only</strong> when
  /// the spreadsheet is reachable but no table by that name exists, so pre-flight
  /// and create-if-absent can branch on a missing table.
  /// </summary>
  /// <exception cref="SheetsSpreadsheetAccessException">
  /// The spreadsheet itself is unreachable — missing or access-denied. This is
  /// distinct from a missing table (a <see langword="null"/> return): Flowthru
  /// creates tables, not spreadsheets, so a missing spreadsheet is a hard
  /// failure rather than a create-if-absent opportunity.
  /// </exception>
  Task<ResolvedTable?> ResolveTable(string spreadsheetId, string tableName, CancellationToken ct);

  /// <summary>
  /// Read the data rows of <paramref name="table"/> (the header row excluded) as
  /// typed neutral field values aligned to its schema. Produces only
  /// <see cref="FieldKind.Number"/>, <see cref="FieldKind.Bool"/>,
  /// <see cref="FieldKind.Text"/>, and <see cref="FieldKind.Empty"/> fields; a
  /// serial date arrives as a <see cref="FieldKind.Number"/>, leaving temporal
  /// coercion to the schema-driven adapter.
  /// </summary>
  Task<TableData> ReadRows(string spreadsheetId, ResolvedTable table, CancellationToken ct);

  /// <summary>
  /// Atomically replace the data rows of <paramref name="table"/> with
  /// <paramref name="rows"/>, preserving the table's header/columns. The prior
  /// data region is cleared and the new typed rows written in a single
  /// all-or-nothing operation, scoped strictly to the table's tab so sibling
  /// tabs (e.g. human-readable formula tabs) are untouched.
  /// </summary>
  Task ReplaceRows(string spreadsheetId, ResolvedTable table, TableData rows, CancellationToken ct);

  /// <summary>
  /// Create a native table named <paramref name="tableName"/> from
  /// <paramref name="schema"/> in <paramref name="spreadsheetId"/>. Used when
  /// <see cref="ResolveTable"/> returns <see langword="null"/>. Returns the
  /// created table resolved to its schema and range.
  /// </summary>
  Task<ResolvedTable> AddTable(string spreadsheetId, string tableName, TableSchema schema, CancellationToken ct);
}
