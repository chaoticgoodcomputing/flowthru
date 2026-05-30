using Flowthru.Data.Storage.Sheets;
using Flowthru.Data.Storage.Sheets.Internal;
using Google.Apis.Sheets.v4.Data;

namespace Flowthru.Extensions.Google.Sheets.Tests;

/// <summary>
/// Unit tests for the pure neutral-tabular ↔ Google-types translator. No
/// <c>SheetsService</c> involved — these assert the translation logic in
/// isolation: column-type strings, the column-index-0 coalesce, serial dates,
/// and field-value mapping.
/// </summary>
[TestFixture]
public sealed class SheetsTranslatorTests
{
  // ── Column-type ↔ Google string (verified tokens) ───────────────────────

  [TestCase(ColumnType.Text, ExpectedResult = "TEXT")]
  [TestCase(ColumnType.Number, ExpectedResult = "DOUBLE")]
  [TestCase(ColumnType.DateTime, ExpectedResult = "DATE_TIME")]
  [TestCase(ColumnType.Date, ExpectedResult = "DATE")]
  [TestCase(ColumnType.Time, ExpectedResult = "TIME")]
  [TestCase(ColumnType.Bool, ExpectedResult = "CHECKBOX")]
  public string ToColumnTypeString_MapsEachNeutralType(ColumnType type) =>
    SheetsTranslator.ToColumnTypeString(type);

  [TestCase("TEXT", ExpectedResult = ColumnType.Text)]
  [TestCase("DOUBLE", ExpectedResult = ColumnType.Number)]
  [TestCase("DATE_TIME", ExpectedResult = ColumnType.DateTime)]
  [TestCase("DATE", ExpectedResult = ColumnType.Date)]
  [TestCase("TIME", ExpectedResult = ColumnType.Time)]
  [TestCase("CHECKBOX", ExpectedResult = ColumnType.Bool)]
  [TestCase("UNKNOWN_TYPE", ExpectedResult = ColumnType.Text)]
  [TestCase(null, ExpectedResult = ColumnType.Text)]
  public ColumnType FromColumnTypeString_MapsBack_FallingBackToText(string? token) =>
    SheetsTranslator.FromColumnTypeString(token);

  // ── Neutral schema → Google Table (AddTable body) ────────────────────────

  [Test]
  public void ToTable_BuildsColumnsWithIndexNameAndType()
  {
    var schema = new TableSchema(new[]
    {
      new TableColumn("Name", ColumnType.Text),
      new TableColumn("Amount", ColumnType.Number),
      new TableColumn("When", ColumnType.DateTime),
    });

    var table = SheetsTranslator.ToTable("FlowthruTable", schema, sheetId: 7);

    Assert.That(table.Name, Is.EqualTo("FlowthruTable"));
    Assert.That(table.Range.SheetId, Is.EqualTo(7));
    Assert.That(table.Range.StartColumnIndex, Is.EqualTo(0));
    Assert.That(table.Range.EndColumnIndex, Is.EqualTo(3));
    Assert.That(table.ColumnProperties, Has.Count.EqualTo(3));

    Assert.That(table.ColumnProperties[0].ColumnIndex, Is.EqualTo(0));
    Assert.That(table.ColumnProperties[0].ColumnName, Is.EqualTo("Name"));
    Assert.That(table.ColumnProperties[0].ColumnType, Is.EqualTo("TEXT"));
    Assert.That(table.ColumnProperties[1].ColumnType, Is.EqualTo("DOUBLE"));
    Assert.That(table.ColumnProperties[2].ColumnType, Is.EqualTo("DATE_TIME"));
    Assert.That(table.ColumnProperties[2].ColumnIndex, Is.EqualTo(2));
  }

  // ── Google Table → ResolvedTable (read-back, columnIndex-0 coalesce) ──────

  [Test]
  public void ToResolvedTable_CoalescesNullColumnIndexToZero()
  {
    // The API omits columnIndex for column 0 (proto3 zero-omission, spike #93):
    // the first column arrives with a null ColumnIndex.
    var table = new Table
    {
      Name = "FlowthruTable",
      Range = new GridRange
      {
        SheetId = 1,
        StartRowIndex = 0,
        EndRowIndex = 5,
        StartColumnIndex = 0,
        EndColumnIndex = 3,
      },
      ColumnProperties = new List<TableColumnProperties>
      {
        new() { ColumnIndex = null, ColumnName = "Name", ColumnType = "TEXT" },
        new() { ColumnIndex = 1, ColumnName = "Amount", ColumnType = "DOUBLE" },
        new() { ColumnIndex = 2, ColumnName = "When", ColumnType = "DATE_TIME" },
      },
    };

    var resolved = SheetsTranslator.ToResolvedTable(table);

    Assert.That(resolved, Is.Not.Null);
    // Null index coalesced to 0, so the columns order by position correctly.
    Assert.That(resolved!.Schema.Columns[0].Name, Is.EqualTo("Name"));
    Assert.That(resolved.Schema.Columns[0].Type, Is.EqualTo(ColumnType.Text));
    Assert.That(resolved.Schema.Columns[1].Type, Is.EqualTo(ColumnType.Number));
    Assert.That(resolved.Schema.Columns[2].Type, Is.EqualTo(ColumnType.DateTime));
    Assert.That(resolved.Range.SheetId, Is.EqualTo(1));
    Assert.That(resolved.Range.EndRowIndex, Is.EqualTo(5));
  }

  [Test]
  public void ToResolvedTable_OrdersColumnsByIndex_WhenOutOfOrder()
  {
    var table = new Table
    {
      Name = "T",
      Range = new GridRange { SheetId = 0, StartRowIndex = 0, EndRowIndex = 1, StartColumnIndex = 0, EndColumnIndex = 2 },
      ColumnProperties = new List<TableColumnProperties>
      {
        new() { ColumnIndex = 1, ColumnName = "Second", ColumnType = "DOUBLE" },
        new() { ColumnIndex = null, ColumnName = "First", ColumnType = "TEXT" },
      },
    };

    var resolved = SheetsTranslator.ToResolvedTable(table);
    Assert.That(resolved!.Schema.Columns[0].Name, Is.EqualTo("First"));
    Assert.That(resolved.Schema.Columns[1].Name, Is.EqualTo("Second"));
  }

  [Test]
  public void ToResolvedTable_NullTableOrRange_ReturnsNull()
  {
    Assert.That(SheetsTranslator.ToResolvedTable(null), Is.Null);
    Assert.That(SheetsTranslator.ToResolvedTable(new Table { Name = "T", Range = null }), Is.Null);
  }

  [Test]
  public void SchemaTable_RoundTrips_ThroughGoogleTable()
  {
    var schema = new TableSchema(new[]
    {
      new TableColumn("Name", ColumnType.Text),
      new TableColumn("Amount", ColumnType.Number),
      new TableColumn("When", ColumnType.DateTime),
    });

    var table = SheetsTranslator.ToTable("RT", schema, sheetId: 3);
    // Simulate the API dropping columnIndex for column 0 on read-back.
    table.ColumnProperties[0].ColumnIndex = null;

    var resolved = SheetsTranslator.ToResolvedTable(table)!;

    Assert.That(resolved.Schema.ColumnCount, Is.EqualTo(3));
    for (var i = 0; i < 3; i++)
    {
      Assert.That(resolved.Schema.Columns[i].Name, Is.EqualTo(schema.Columns[i].Name));
      Assert.That(resolved.Schema.Columns[i].Type, Is.EqualTo(schema.Columns[i].Type));
    }
  }

  // ── Neutral FieldValue → ExtendedValue ───────────────────────────────────

  [Test]
  public void ToCellData_Number_SetsNumberValue()
  {
    var cell = SheetsTranslator.ToCellData(FieldValue.Number(42.5));
    Assert.That(cell.UserEnteredValue.NumberValue, Is.EqualTo(42.5));
    Assert.That(cell.UserEnteredValue.BoolValue, Is.Null);
    Assert.That(cell.UserEnteredValue.StringValue, Is.Null);
    Assert.That(cell.UserEnteredFormat, Is.Null);
  }

  [Test]
  public void ToCellData_Bool_SetsBoolValue()
  {
    var cell = SheetsTranslator.ToCellData(FieldValue.Bool(true));
    Assert.That(cell.UserEnteredValue.BoolValue, Is.True);
    Assert.That(cell.UserEnteredValue.NumberValue, Is.Null);
  }

  [Test]
  public void ToCellData_Text_SetsStringValue()
  {
    var cell = SheetsTranslator.ToCellData(FieldValue.Text("hello"));
    Assert.That(cell.UserEnteredValue.StringValue, Is.EqualTo("hello"));
  }

  [Test]
  public void ToCellData_Empty_HasNoUserEnteredValue()
  {
    var cell = SheetsTranslator.ToCellData(FieldValue.Empty);
    Assert.That(cell.UserEnteredValue, Is.Null);
    Assert.That(cell.UserEnteredFormat, Is.Null);
  }

  // ── Serial date: DateTime → serial + numberFormat ───────────────────────

  [Test]
  public void ToCellData_Temporal_DateTime_EmitsSerialAndDateTimeFormat()
  {
    // 1899-12-30 is serial 0; one full day later is serial 1.
    var dt = new DateTime(1899, 12, 31, 0, 0, 0, DateTimeKind.Unspecified);
    var cell = SheetsTranslator.ToCellData(FieldValue.Temporal(dt, TemporalKind.DateTime));

    Assert.That(cell.UserEnteredValue.NumberValue, Is.EqualTo(1.0).Within(1e-9));
    Assert.That(cell.UserEnteredFormat.NumberFormat.Type, Is.EqualTo("DATE_TIME"));
  }

  [Test]
  public void ToCellData_Temporal_Date_EmitsDateFormat()
  {
    var cell = SheetsTranslator.ToCellData(
      FieldValue.Temporal(new DateTime(2020, 1, 1), TemporalKind.Date));
    Assert.That(cell.UserEnteredFormat.NumberFormat.Type, Is.EqualTo("DATE"));
  }

  [Test]
  public void ToCellData_Temporal_Time_EmitsTimeFormat()
  {
    var cell = SheetsTranslator.ToCellData(
      FieldValue.Temporal(new DateTime(2020, 1, 1, 13, 30, 0), TemporalKind.Time));
    Assert.That(cell.UserEnteredFormat.NumberFormat.Type, Is.EqualTo("TIME"));
  }

  [Test]
  public void SerialDate_RoundTrips_WithTimeComponent()
  {
    var original = new DateTime(2024, 3, 15, 9, 45, 30, DateTimeKind.Unspecified);
    var serial = SheetsTranslator.ToSerial(original);
    var back = SheetsTranslator.FromSerial(serial);
    Assert.That(back, Is.EqualTo(original).Within(TimeSpan.FromMilliseconds(1)));
  }

  // ── Google read result → neutral FieldValue ──────────────────────────────

  [Test]
  public void FromRawValue_SerialNumber_BecomesNumberField_NotTemporal()
  {
    // A serial date read back from the values API arrives as a double; the
    // gateway must surface it as Number, leaving temporal coercion to the
    // schema-driven adapter.
    var field = SheetsTranslator.FromRawValue(45000.5);
    Assert.That(field.Kind, Is.EqualTo(FieldKind.Number));
    Assert.That(field.NumberValue, Is.EqualTo(45000.5));
  }

  [Test]
  public void FromRawValue_HandlesEachRawKind()
  {
    Assert.That(SheetsTranslator.FromRawValue(true).Kind, Is.EqualTo(FieldKind.Bool));
    Assert.That(SheetsTranslator.FromRawValue("text").Kind, Is.EqualTo(FieldKind.Text));
    Assert.That(SheetsTranslator.FromRawValue(3.14).Kind, Is.EqualTo(FieldKind.Number));
    Assert.That(SheetsTranslator.FromRawValue(7L).Kind, Is.EqualTo(FieldKind.Number));
    Assert.That(SheetsTranslator.FromRawValue(null).Kind, Is.EqualTo(FieldKind.Empty));
    Assert.That(SheetsTranslator.FromRawValue("").Kind, Is.EqualTo(FieldKind.Empty));
  }

  [Test]
  public void FromValueRange_NullValues_YieldsEmptyBodyUnderSchema()
  {
    var schema = new TableSchema(new[] { new TableColumn("A", ColumnType.Text) });
    var data = SheetsTranslator.FromValueRange(schema, new ValueRange { Values = null });
    Assert.That(data.RowCount, Is.EqualTo(0));
    Assert.That(data.Schema, Is.SameAs(schema));
  }

  [Test]
  public void FromValueRange_TranslatesRowsAndColumns()
  {
    var schema = new TableSchema(new[]
    {
      new TableColumn("S", ColumnType.Text),
      new TableColumn("N", ColumnType.Number),
      new TableColumn("B", ColumnType.Bool),
    });
    var range = new ValueRange
    {
      Values = new List<IList<object>>
      {
        new List<object> { "a", 1.0, true },
        new List<object> { "b", 2.0, false },
      },
    };

    var data = SheetsTranslator.FromValueRange(schema, range);

    Assert.That(data.RowCount, Is.EqualTo(2));
    Assert.That(data.Rows[0][0], Is.EqualTo(FieldValue.Text("a")));
    Assert.That(data.Rows[0][1], Is.EqualTo(FieldValue.Number(1.0)));
    Assert.That(data.Rows[1][2], Is.EqualTo(FieldValue.Bool(false)));
  }

  // ── ToRowData padding ───────────────────────────────────────────────────

  [Test]
  public void ToRowData_PadsShortRowsWithEmptyCells()
  {
    var row = new[] { FieldValue.Text("x") };
    var rowData = SheetsTranslator.ToRowData(row, width: 3);
    Assert.That(rowData.Values, Has.Count.EqualTo(3));
    Assert.That(rowData.Values[1].UserEnteredValue, Is.Null);
    Assert.That(rowData.Values[2].UserEnteredValue, Is.Null);
  }
}
