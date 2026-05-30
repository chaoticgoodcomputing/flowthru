namespace Flowthru.Data.Storage.Sheets;

/// <summary>
/// The Flowthru-neutral grid extent a table occupies on its tab — the sheet it
/// lives on plus the half-open row/column bounds of the whole table (header row
/// included). Enough to scope an atomic data-row replace to exactly this table,
/// without exposing any Google SDK type.
/// </summary>
/// <param name="SheetId">
/// The stable numeric id of the tab the table lives on. Required to scope a
/// <c>batchUpdate</c> to exactly this tab so sibling tabs are untouched.
/// </param>
/// <param name="StartRowIndex">First row of the table, inclusive (0-based). The header row.</param>
/// <param name="EndRowIndex">One past the last row of the table (half-open).</param>
/// <param name="StartColumnIndex">First column of the table, inclusive (0-based).</param>
/// <param name="EndColumnIndex">One past the last column of the table (half-open).</param>
public sealed record TableRange(
  int SheetId,
  int StartRowIndex,
  int EndRowIndex,
  int StartColumnIndex,
  int EndColumnIndex);

/// <summary>
/// A table resolved by name: its schema (the column names and types read back
/// from the native table) and the grid range it occupies. Returned by
/// <see cref="ISheetsGateway.ResolveTable"/>; <see langword="null"/> there means
/// the table does not exist, letting pre-flight and create-if-absent branch.
/// </summary>
/// <param name="Name">The table's name — its stable catalog-item identity.</param>
/// <param name="Schema">The columns and types the live table carries.</param>
/// <param name="Range">The grid extent the table occupies, header included.</param>
public sealed record ResolvedTable(
  string Name,
  TableSchema Schema,
  TableRange Range);
