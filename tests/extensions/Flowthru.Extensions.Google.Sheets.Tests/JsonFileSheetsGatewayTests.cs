using System.Text.Json;
using Flowthru.Data.Storage.Sheets;
using Flowthru.Data.Storage.Sheets.Local;

namespace Flowthru.Extensions.Google.Sheets.Tests;

/// <summary>
/// Behavior unique to <see cref="JsonFileSheetsGateway"/> — the file lifecycle the
/// shared <see cref="LocalSheetsStore"/> does not own: load-on-construct,
/// flush-on-write, snapshot reads, missing file → empty, corrupt JSON → throw,
/// and the null <c>FlowResource</c>. The op semantics themselves are covered by
/// the store-behavior suite.
/// </summary>
[TestFixture]
public sealed class JsonFileSheetsGatewayTests
{
  private const string SpreadsheetId = "sheet-1";
  private const string TableName = "RawData";

  private string _path = null!;

  [SetUp]
  public void SetUp()
  {
    _path = Path.Combine(Path.GetTempPath(), $"flowthru-jsonfile-{Guid.NewGuid():N}.json");
  }

  [TearDown]
  public void TearDown()
  {
    if (File.Exists(_path)) File.Delete(_path);
  }

  private static TableSchema Schema() =>
    new(new[] { new TableColumn("Name", ColumnType.Text) });

  private static TableData OneRow(TableSchema schema) =>
    new(schema, new[] { new[] { FieldValue.Text("x") } });

  [Test]
  public void MissingFile_StartsEmpty_NoFileUntilFirstWrite()
  {
    var gateway = new JsonFileSheetsGateway(_path);

    // A fresh gateway over a nonexistent path is an empty, reachable store: no
    // file is created just by reading.
    Assert.That(File.Exists(_path), Is.False);
    Assert.ThrowsAsync<SheetsSpreadsheetAccessException>(
      async () => await gateway.ResolveTable(SpreadsheetId, TableName, default));
  }

  [Test]
  public void CorruptJson_Throws_OnConstruct()
  {
    File.WriteAllText(_path, "{ this is not valid json ");

    Assert.Throws<JsonException>(() => _ = new JsonFileSheetsGateway(_path));
  }

  [Test]
  public void RegisterSpreadsheet_FlushesToFile()
  {
    var gateway = new JsonFileSheetsGateway(_path);
    gateway.RegisterSpreadsheet(SpreadsheetId);

    Assert.That(File.Exists(_path), Is.True);
    Assert.That(File.ReadAllText(_path), Does.Contain(SpreadsheetId));
  }

  [Test]
  public async Task AddTable_And_ReplaceRows_FlushOnWrite_AndReloadFromDisk()
  {
    var gateway = new JsonFileSheetsGateway(_path);
    gateway.RegisterSpreadsheet(SpreadsheetId);
    var created = await gateway.AddTable(SpreadsheetId, TableName, Schema(), default);
    await gateway.ReplaceRows(SpreadsheetId, created, OneRow(created.Schema), default);

    // A brand-new gateway over the same path sees everything the first one wrote
    // — proving each mutating op flushed the full store to disk.
    var reloaded = new JsonFileSheetsGateway(_path);
    var resolved = await reloaded.ResolveTable(SpreadsheetId, TableName, default);
    Assert.That(resolved, Is.Not.Null);
    var read = await reloaded.ReadRows(SpreadsheetId, resolved!, default);
    Assert.That(read.RowCount, Is.EqualTo(1));
    Assert.That(read.Rows[0][0], Is.EqualTo(FieldValue.Text("x")));
  }

  [Test]
  public async Task Seed_FlushesToFile_AndIsReadableByFreshGateway()
  {
    var gateway = new JsonFileSheetsGateway(_path);
    gateway.Seed(SpreadsheetId, TableName, Schema(), new[]
    {
      (IReadOnlyList<FieldValue>)new[] { FieldValue.Text("seeded") },
    });

    var reloaded = new JsonFileSheetsGateway(_path);
    var resolved = await reloaded.ResolveTable(SpreadsheetId, TableName, default);
    var read = await reloaded.ReadRows(SpreadsheetId, resolved!, default);
    Assert.That(read.Rows[0][0], Is.EqualTo(FieldValue.Text("seeded")));
  }

  [Test]
  public async Task Reads_ServeFromSnapshot_NotRereadingFilePerOp()
  {
    var gateway = new JsonFileSheetsGateway(_path);
    gateway.Seed(SpreadsheetId, TableName, Schema(), new[]
    {
      (IReadOnlyList<FieldValue>)new[] { FieldValue.Text("original") },
    });

    // Corrupt the file out from under the gateway. Because reads serve from the
    // in-memory snapshot loaded on construct (not a per-op re-read), the gateway
    // keeps reading the original data without tripping on the now-bad file.
    File.WriteAllText(_path, "garbage that would fail to parse");

    var resolved = await gateway.ResolveTable(SpreadsheetId, TableName, default);
    var read = await gateway.ReadRows(SpreadsheetId, resolved!, default);
    Assert.That(read.Rows[0][0], Is.EqualTo(FieldValue.Text("original")));
  }

  [Test]
  public async Task ReplaceRows_IsAtomicReplace_OnDisk()
  {
    var gateway = new JsonFileSheetsGateway(_path);
    gateway.RegisterSpreadsheet(SpreadsheetId);
    var created = await gateway.AddTable(SpreadsheetId, TableName, Schema(), default);
    await gateway.ReplaceRows(
      SpreadsheetId, created,
      new TableData(created.Schema, new[] { new[] { FieldValue.Text("a") } }), default);
    await gateway.ReplaceRows(
      SpreadsheetId, created,
      new TableData(created.Schema, new[] { new[] { FieldValue.Text("b") } }), default);

    var reloaded = new JsonFileSheetsGateway(_path);
    var resolved = await reloaded.ResolveTable(SpreadsheetId, TableName, default);
    var read = await reloaded.ReadRows(SpreadsheetId, resolved!, default);
    Assert.That(read.RowCount, Is.EqualTo(1));
    Assert.That(read.Rows[0][0], Is.EqualTo(FieldValue.Text("b")));
  }

  [Test]
  public void FlowResource_IsNull()
  {
    var gateway = new JsonFileSheetsGateway(_path);
    Assert.That(gateway.FlowResource, Is.Null);
  }
}
