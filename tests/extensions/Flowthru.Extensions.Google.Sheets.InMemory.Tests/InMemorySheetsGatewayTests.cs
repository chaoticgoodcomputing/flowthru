using Flowthru.Data.Storage.Sheets;
using Flowthru.Data.Storage.Sheets.InMemory;

namespace Flowthru.Extensions.Google.Sheets.InMemory.Tests;

/// <summary>
/// Quota-OFF behavior of <see cref="InMemorySheetsGateway"/> — the deterministic
/// default the starter example relies on: the four seam ops round-trip, absent
/// tables resolve to null, duplicate creation throws, and the store seeds/dumps
/// as JSON.
/// </summary>
[TestFixture]
public sealed class InMemorySheetsGatewayTests
{
  private const string SpreadsheetId = "sheet-1";
  private const string TableName = "RawData";

  private static TableSchema Schema() => new(new[]
  {
    new TableColumn("Name", ColumnType.Text),
    new TableColumn("Amount", ColumnType.Number),
    new TableColumn("Active", ColumnType.Bool),
  });

  private static IReadOnlyList<FieldValue> Row(string name, double amount, bool active) =>
    new[] { FieldValue.Text(name), FieldValue.Number(amount), FieldValue.Bool(active) };

  [Test]
  public async Task AddTable_ReplaceRows_ReadRows_RoundTrips()
  {
    var gateway = new InMemorySheetsGateway();

    var created = await gateway.AddTable(SpreadsheetId, TableName, Schema(), default);
    Assert.That(created.Name, Is.EqualTo(TableName));
    Assert.That(created.Schema.ColumnCount, Is.EqualTo(3));
    // Freshly created: header only, no data rows -> half-open end row = 1.
    Assert.That(created.Range.EndRowIndex, Is.EqualTo(1));
    Assert.That(created.Range.EndColumnIndex, Is.EqualTo(3));

    var data = new TableData(created.Schema, new[]
    {
      Row("alice", 1.5, true),
      Row("bob", 2.0, false),
    });
    await gateway.ReplaceRows(SpreadsheetId, created, data, default);

    var resolved = await gateway.ResolveTable(SpreadsheetId, TableName, default);
    Assert.That(resolved, Is.Not.Null);
    // Range now reflects the two data rows beneath the header.
    Assert.That(resolved!.Range.EndRowIndex, Is.EqualTo(3));

    var read = await gateway.ReadRows(SpreadsheetId, resolved, default);
    Assert.That(read.RowCount, Is.EqualTo(2));
    Assert.That(read.Rows[0][0], Is.EqualTo(FieldValue.Text("alice")));
    Assert.That(read.Rows[0][1], Is.EqualTo(FieldValue.Number(1.5)));
    Assert.That(read.Rows[1][2], Is.EqualTo(FieldValue.Bool(false)));
  }

  [Test]
  public async Task ReplaceRows_IsAtomicReplace_PreservingSchema()
  {
    var gateway = new InMemorySheetsGateway();
    var created = await gateway.AddTable(SpreadsheetId, TableName, Schema(), default);

    await gateway.ReplaceRows(
      SpreadsheetId, created, new TableData(created.Schema, new[] { Row("a", 1, true) }), default);
    // Replace wholesale with a different row set; old rows must be gone.
    await gateway.ReplaceRows(
      SpreadsheetId, created, new TableData(created.Schema, new[] { Row("z", 9, false) }), default);

    var read = await gateway.ReadRows(SpreadsheetId, created, default);
    Assert.That(read.RowCount, Is.EqualTo(1));
    Assert.That(read.Rows[0][0], Is.EqualTo(FieldValue.Text("z")));
    // Schema columns preserved across the replace.
    Assert.That(read.Schema.Columns[2].Name, Is.EqualTo("Active"));
  }

  [Test]
  public async Task ReplaceRows_ToEmpty_ClearsAllDataRows()
  {
    var gateway = new InMemorySheetsGateway();
    var created = await gateway.AddTable(SpreadsheetId, TableName, Schema(), default);
    await gateway.ReplaceRows(
      SpreadsheetId, created, new TableData(created.Schema, new[] { Row("a", 1, true) }), default);

    await gateway.ReplaceRows(SpreadsheetId, created, TableData.Empty(created.Schema), default);

    var read = await gateway.ReadRows(SpreadsheetId, created, default);
    Assert.That(read.RowCount, Is.EqualTo(0));
  }

  [Test]
  public async Task ResolveTable_ReturnsNull_WhenSpreadsheetAbsent()
  {
    var gateway = new InMemorySheetsGateway();
    var resolved = await gateway.ResolveTable("never-seen", "nope", default);
    Assert.That(resolved, Is.Null);
  }

  [Test]
  public async Task ResolveTable_ReturnsNull_WhenTableAbsent()
  {
    var gateway = new InMemorySheetsGateway();
    await gateway.AddTable(SpreadsheetId, TableName, Schema(), default);

    var resolved = await gateway.ResolveTable(SpreadsheetId, "OtherTable", default);
    Assert.That(resolved, Is.Null);
  }

  [Test]
  public async Task AddTable_OnExistingName_Throws()
  {
    var gateway = new InMemorySheetsGateway();
    await gateway.AddTable(SpreadsheetId, TableName, Schema(), default);

    Assert.That(
      async () => await gateway.AddTable(SpreadsheetId, TableName, Schema(), default),
      Throws.InstanceOf<InvalidOperationException>());
  }

  [Test]
  public async Task Seed_RegistersTableWithRows()
  {
    var gateway = new InMemorySheetsGateway();
    gateway.Seed(SpreadsheetId, TableName, Schema(), new[] { Row("seeded", 42, true) });

    var resolved = await gateway.ResolveTable(SpreadsheetId, TableName, default);
    Assert.That(resolved, Is.Not.Null);

    var read = await gateway.ReadRows(SpreadsheetId, resolved!, default);
    Assert.That(read.RowCount, Is.EqualTo(1));
    Assert.That(read.Rows[0][0], Is.EqualTo(FieldValue.Text("seeded")));
  }

  [Test]
  public async Task JsonSeedAndDump_RoundTrips()
  {
    var source = new InMemorySheetsGateway();
    source.Seed(SpreadsheetId, TableName, Schema(), new[]
    {
      Row("alice", 1.5, true),
      Row("bob", 2.0, false),
    });

    var json = source.ToJson();

    // Rehydrate a fresh gateway from the JSON dump and read it back.
    var loaded = new InMemorySheetsGateway(InMemorySheetsStore.FromJson(json));
    var resolved = await loaded.ResolveTable(SpreadsheetId, TableName, default);
    Assert.That(resolved, Is.Not.Null);
    Assert.That(resolved!.Schema.Columns[1].Type, Is.EqualTo(ColumnType.Number));

    var read = await loaded.ReadRows(SpreadsheetId, resolved, default);
    Assert.That(read.RowCount, Is.EqualTo(2));
    Assert.That(read.Rows[1][0], Is.EqualTo(FieldValue.Text("bob")));

    // Dumping the rehydrated store reproduces the original JSON exactly.
    Assert.That(loaded.ToJson(), Is.EqualTo(json));
  }

  [Test]
  public async Task TemporalField_SeededAndDumped_RoundTrips()
  {
    var schema = new TableSchema(new[] { new TableColumn("When", ColumnType.DateTime) });
    var when = new DateTime(2024, 6, 1, 12, 0, 0);
    var gateway = new InMemorySheetsGateway();
    gateway.Seed(SpreadsheetId, "Events", schema, new[]
    {
      new[] { FieldValue.Temporal(when, TemporalKind.DateTime) },
    });

    var loaded = new InMemorySheetsGateway(InMemorySheetsStore.FromJson(gateway.ToJson()));
    var resolved = await loaded.ResolveTable(SpreadsheetId, "Events", default);
    var read = await loaded.ReadRows(SpreadsheetId, resolved!, default);

    Assert.That(read.Rows[0][0], Is.EqualTo(FieldValue.Temporal(when, TemporalKind.DateTime)));
  }
}
