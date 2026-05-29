using Flowthru.Data.Storage.Sheets;
using Flowthru.Data.Storage.Sheets.Internal;

namespace Flowthru.Extensions.Google.Sheets.Tests;

/// <summary>
/// Tests for the two table-scoped batches the translator assembles: the atomic
/// data-row replace (scoped to one sheet id and the table's data region,
/// leaving the header intact) and the AddTable request.
/// </summary>
[TestFixture]
public sealed class ReplaceBatchTests
{
  private static TableSchema Schema(int columns) =>
    new(Enumerable.Range(0, columns)
      .Select(i => new TableColumn($"c{i}", ColumnType.Text))
      .ToList());

  // A table on sheet 1234, columns [0,3), rows [0,11): header at row 0, data
  // rows [1,11).
  private static ResolvedTable Table(
    int sheetId = 1234, int startRow = 0, int endRow = 11, int startCol = 0, int endCol = 3) =>
    new("T", Schema(endCol - startCol),
      new TableRange(sheetId, startRow, endRow, startCol, endCol));

  private static TableData Rows(ResolvedTable table, params FieldValue[][] rows) =>
    new(table.Schema, rows.Select(r => (IReadOnlyList<FieldValue>)r).ToList());

  [Test]
  public void BuildReplaceBatch_ClearAndWrite_AreScopedToSingleSheetId()
  {
    var table = Table(sheetId: 1234);
    var rows = Rows(table,
      new[] { FieldValue.Number(1), FieldValue.Number(2), FieldValue.Number(3) });

    var batch = SheetsTranslator.BuildReplaceBatch(table, rows);

    Assert.That(batch.Requests, Has.Count.EqualTo(2), "one clear + one write");
    foreach (var req in batch.Requests)
    {
      var range = req.UpdateCells.Range;
      var start = req.UpdateCells.Start;
      var resolvedSheetId = range?.SheetId ?? start?.SheetId;
      Assert.That(resolvedSheetId, Is.EqualTo(1234),
        "every request targets the resolved table's sheet, never a sibling");
    }
  }

  [Test]
  public void BuildReplaceBatch_ClearRequest_CoversDataRegionBelowHeader()
  {
    var table = Table(sheetId: 7, startRow: 0, endRow: 8, startCol: 0, endCol: 3);
    var batch = SheetsTranslator.BuildReplaceBatch(table,
      Rows(table, new[] { FieldValue.Text("x"), FieldValue.Text("y"), FieldValue.Text("z") }));

    var clear = batch.Requests[0].UpdateCells;
    // Data region starts one row below the header (row 0), runs to EndRowIndex.
    Assert.That(clear.Range.StartRowIndex, Is.EqualTo(1), "header row preserved");
    Assert.That(clear.Range.EndRowIndex, Is.EqualTo(8));
    Assert.That(clear.Range.StartColumnIndex, Is.EqualTo(0));
    Assert.That(clear.Range.EndColumnIndex, Is.EqualTo(3));
    Assert.That((string)clear.Fields, Is.EqualTo(SheetsTranslator.ClearFieldsMask));
    Assert.That(clear.Rows, Is.Null, "clear carries no rows");
  }

  [Test]
  public void BuildReplaceBatch_WriteRequest_AnchorsBelowHeaderAtStartColumn()
  {
    // Table not at the origin: header at row 2, columns [1,3).
    var table = Table(sheetId: 7, startRow: 2, endRow: 5, startCol: 1, endCol: 3);
    var batch = SheetsTranslator.BuildReplaceBatch(table,
      Rows(table, new[] { FieldValue.Text("a"), FieldValue.Number(2) }));

    var write = batch.Requests[1].UpdateCells;
    Assert.That(write.Start.RowIndex, Is.EqualTo(3), "data starts one row below the header");
    Assert.That(write.Start.ColumnIndex, Is.EqualTo(1), "anchored at the table's start column");
    Assert.That((string)write.Fields, Is.EqualTo(SheetsTranslator.WriteFieldsMask));
    Assert.That(write.Rows, Has.Count.EqualTo(1));
    Assert.That(write.Rows[0].Values, Has.Count.EqualTo(2));
  }

  [Test]
  public void BuildReplaceBatch_EmptyPriorDataRegion_SkipsClearRequest()
  {
    // Header-only table (endRow == startRow + 1): no data region to clear.
    var table = Table(sheetId: 7, startRow: 0, endRow: 1, startCol: 0, endCol: 1);
    var batch = SheetsTranslator.BuildReplaceBatch(table,
      Rows(table, new[] { FieldValue.Text("a") }));

    Assert.That(batch.Requests, Has.Count.EqualTo(1));
    Assert.That(batch.Requests[0].UpdateCells.Start, Is.Not.Null, "the sole request is the write");
  }

  [Test]
  public void BuildReplaceBatch_EmptyRows_OnlyClears()
  {
    var table = Table(sheetId: 7, startRow: 0, endRow: 5, startCol: 0, endCol: 2);
    var batch = SheetsTranslator.BuildReplaceBatch(table, TableData.Empty(table.Schema));

    Assert.That(batch.Requests, Has.Count.EqualTo(1));
    Assert.That(batch.Requests[0].UpdateCells.Range, Is.Not.Null, "the sole request is the clear");
  }

  [Test]
  public void BuildReplaceBatch_NothingToDo_YieldsEmptyRequestList()
  {
    // Header-only table + no rows: nothing to clear, nothing to write.
    var table = Table(sheetId: 7, startRow: 0, endRow: 1, startCol: 0, endCol: 1);
    var batch = SheetsTranslator.BuildReplaceBatch(table, TableData.Empty(table.Schema));
    Assert.That(batch.Requests, Is.Empty);
  }

  // ── AddTable batch ────────────────────────────────────────────────────────

  [Test]
  public void BuildAddTableBatch_CarriesOneAddTableRequestFromSchema()
  {
    var schema = new TableSchema(new[]
    {
      new TableColumn("Name", ColumnType.Text),
      new TableColumn("Amount", ColumnType.Number),
    });

    var batch = SheetsTranslator.BuildAddTableBatch("Created", schema, sheetId: 9);

    Assert.That(batch.Requests, Has.Count.EqualTo(1));
    var add = batch.Requests[0].AddTable;
    Assert.That(add, Is.Not.Null);
    Assert.That(add.Table.Name, Is.EqualTo("Created"));
    Assert.That(add.Table.Range.SheetId, Is.EqualTo(9));
    Assert.That(add.Table.ColumnProperties, Has.Count.EqualTo(2));
    Assert.That(add.Table.ColumnProperties[0].ColumnType, Is.EqualTo("TEXT"));
    Assert.That(add.Table.ColumnProperties[1].ColumnType, Is.EqualTo("DOUBLE"));
  }
}
