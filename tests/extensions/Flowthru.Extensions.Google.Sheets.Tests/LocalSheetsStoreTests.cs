using System.Text.Json;
using Flowthru.Data.Storage.Sheets;
using Flowthru.Data.Storage.Sheets.Local;

namespace Flowthru.Extensions.Google.Sheets.Tests;

/// <summary>
/// Direct tests of the shared <see cref="LocalSheetsStore"/> for the paths the
/// gateway suites do not exercise head-on: JSON error handling and reading a
/// table that does not exist on a reachable spreadsheet.
/// </summary>
[TestFixture]
public sealed class LocalSheetsStoreTests
{
  private const string SpreadsheetId = "sheet-1";

  [Test]
  public void FromJson_OnMalformedJson_ThrowsJsonException()
  {
    Assert.Throws<JsonException>(() => LocalSheetsStore.FromJson("{ not valid "));
  }

  [Test]
  public void FromJson_OnNullDocument_ThrowsArgumentException()
  {
    // Syntactically valid JSON that deserializes to a null document.
    Assert.Throws<ArgumentException>(() => LocalSheetsStore.FromJson("null"));
  }

  [Test]
  public void ReadRows_OnAbsentTable_ButReachableSpreadsheet_Throws()
  {
    var store = new LocalSheetsStore();
    store.RegisterSpreadsheet(SpreadsheetId);
    var schema = new TableSchema(new[] { new TableColumn("Name", ColumnType.Text) });
    var created = store.AddTable(SpreadsheetId, "Present", schema);

    // A ResolvedTable that names a table not actually in the store: the read must
    // surface the missing-table failure rather than silently returning rows.
    var ghost = created with { Name = "Absent" };
    Assert.Throws<InvalidOperationException>(() => store.ReadRows(SpreadsheetId, ghost));
  }

  [Test]
  public void RegisterSpreadsheet_IsIdempotent_KeepsExistingTables()
  {
    var store = new LocalSheetsStore();
    var schema = new TableSchema(new[] { new TableColumn("Name", ColumnType.Text) });
    store.Seed(SpreadsheetId, "T", schema);

    // Re-registering must not wipe the already-seeded table.
    store.RegisterSpreadsheet(SpreadsheetId);

    Assert.That(store.ResolveTable(SpreadsheetId, "T"), Is.Not.Null);
  }
}
