using Flowthru.Data.Storage.Sheets;
using Flowthru.Data.Storage.Sheets.InMemory;

namespace Flowthru.Extensions.Google.Sheets.InMemory.Tests;

/// <summary>
/// Read-side date fidelity of <see cref="InMemorySheetsGateway"/>: the live
/// <c>SheetsServiceGateway</c> reads with <c>UNFORMATTED_VALUE</c> +
/// <c>SERIAL_NUMBER</c>, so Date/DateTime/Time columns always come back as a
/// serial <see cref="FieldKind.Number"/> — never a <see cref="FieldKind.Temporal"/>.
/// The double mirrors that: it normalizes a temporal column to its serial Number
/// on read regardless of whether the cell was seeded as a Temporal or as a raw
/// serial Number, so a test or example can seed dates the natural way and still
/// be faithful to production.
/// </summary>
[TestFixture]
public sealed class DateReadFidelityTests
{
  private const string SpreadsheetId = "ss-dates";
  private const string TableName = "Events";

  // 2024-06-01T12:00:00 → its Sheets serial (days since the 1899-12-30 epoch).
  private static readonly DateTime When = new(2024, 6, 1, 12, 0, 0);
  private static double Serial(DateTime dt) => (dt - new DateTime(1899, 12, 30)).TotalDays;

  private static async Task<FieldValue> ReadFirstCell(
    InMemorySheetsGateway gateway, ColumnType columnType, FieldValue seeded)
  {
    var schema = new TableSchema(new[] { new TableColumn("When", columnType) });
    gateway.Seed(SpreadsheetId, TableName, schema, new[] { new[] { seeded } });
    var resolved = await gateway.ResolveTable(SpreadsheetId, TableName, default);
    var data = await gateway.ReadRows(SpreadsheetId, resolved!, default);
    return data.Rows[0][0];
  }

  [TestCase(ColumnType.Date)]
  [TestCase(ColumnType.DateTime)]
  [TestCase(ColumnType.Time)]
  public async Task TemporalSeed_ReadsBackAsSerialNumber(ColumnType columnType)
  {
    var gateway = new InMemorySheetsGateway();
    var read = await ReadFirstCell(
      gateway, columnType, FieldValue.Temporal(When, TemporalKind.DateTime));

    Assert.That(read.Kind, Is.EqualTo(FieldKind.Number),
      "a temporal column never reads back as Temporal — the live API returns a serial");
    Assert.That(read.NumberValue, Is.EqualTo(Serial(When)));
  }

  [TestCase(ColumnType.Date)]
  [TestCase(ColumnType.DateTime)]
  [TestCase(ColumnType.Time)]
  public async Task NumberSeed_ReadsBackUnchanged(ColumnType columnType)
  {
    var serial = Serial(When);
    var gateway = new InMemorySheetsGateway();
    var read = await ReadFirstCell(gateway, columnType, FieldValue.Number(serial));

    Assert.That(read, Is.EqualTo(FieldValue.Number(serial)),
      "a cell already stored as a serial is the live representation already");
  }

  [Test]
  public async Task TemporalAndNumberSeed_RoundTripIdentically()
  {
    var serial = Serial(When);

    var fromTemporal = await ReadFirstCell(
      new InMemorySheetsGateway(), ColumnType.Date,
      FieldValue.Temporal(When, TemporalKind.Date));
    var fromNumber = await ReadFirstCell(
      new InMemorySheetsGateway(), ColumnType.Date,
      FieldValue.Number(serial));

    Assert.That(fromTemporal, Is.EqualTo(fromNumber),
      "seeding a date as Temporal or as a serial Number must be indistinguishable on read");
  }

  [Test]
  public async Task NonTemporalColumns_AreNotNormalized()
  {
    // A Number in a plain Number column, and text in a Text column, must pass
    // through verbatim — only temporal columns are coerced to serial.
    var schema = new TableSchema(new[]
    {
      new TableColumn("Label", ColumnType.Text),
      new TableColumn("Amount", ColumnType.Number),
      new TableColumn("Active", ColumnType.Bool),
    });
    var gateway = new InMemorySheetsGateway();
    gateway.Seed(SpreadsheetId, TableName, schema, new[]
    {
      new[] { FieldValue.Text("hi"), FieldValue.Number(3.5), FieldValue.Bool(true) },
    });

    var resolved = await gateway.ResolveTable(SpreadsheetId, TableName, default);
    var row = (await gateway.ReadRows(SpreadsheetId, resolved!, default)).Rows[0];

    Assert.Multiple(() =>
    {
      Assert.That(row[0], Is.EqualTo(FieldValue.Text("hi")));
      Assert.That(row[1], Is.EqualTo(FieldValue.Number(3.5)));
      Assert.That(row[2], Is.EqualTo(FieldValue.Bool(true)));
    });
  }

  [Test]
  public async Task EmptyTemporalCell_StaysEmpty()
  {
    // A missing value in a date column is still "no value" — normalization only
    // touches Temporal cells, leaving Empty alone (matching a blank live cell).
    var gateway = new InMemorySheetsGateway();
    var read = await ReadFirstCell(gateway, ColumnType.DateTime, FieldValue.Empty);

    Assert.That(read, Is.EqualTo(FieldValue.Empty));
  }
}
