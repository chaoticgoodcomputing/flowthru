using Flowthru.Data.Catalog;
using Flowthru.Data.Schema;
using Flowthru.Data.Storage.Sheets;
using Flowthru.Data.Storage.Sheets.InMemory;
using Flowthru.Prelude;

namespace Flowthru.Extensions.Google.Sheets.Tests;

/// <summary>
/// End-to-end tests for the <c>ItemFactory.Enumerable.GoogleSheets&lt;TRow&gt;</c>
/// smart constructor, driven against the offline
/// <see cref="InMemorySheetsGateway"/> — no live Google API. Proves the
/// factory-built <see cref="IItem{T}"/> round-trips load and save through the
/// gateway it is handed.
/// </summary>
[TestFixture]
public sealed class GoogleSheetsItemFactoryTests
{
  private const string SpreadsheetId = "ss-factory";
  private const string TableName = "Widgets";

  public sealed class Widget : IFlatSchema
  {
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public double Price { get; set; }
  }

  private static async Task<A> Expect<A>(FlowIO<A> io)
  {
    var result = await io.Run();
    if (result is EffResult<A>.Failure failure)
    {
      Assert.Fail($"Expected success, got failure: {failure.Error.Message}");
    }
    return ((EffResult<A>.Success)result).Value;
  }

  [Test]
  public async Task GoogleSheets_BuildsItem_SaveThenLoadRoundTrips()
  {
    var gateway = new InMemorySheetsGateway();
    // Flowthru creates tables, not spreadsheets — the spreadsheet must exist.
    gateway.RegisterSpreadsheet(SpreadsheetId);

    var item = ItemFactory.Enumerable.GoogleSheets<Widget>(
      "widgets", SpreadsheetId, TableName, gateway);

    var written = new[]
    {
      new Widget { Name = "bolt", Quantity = 12, Price = 0.25 },
      new Widget { Name = "nut", Quantity = 30, Price = 0.10 },
    };

    // Save creates the table from TRow (it is absent) then replaces its rows.
    await Expect(item.Save(written));

    var read = (await Expect(item.Load())).ToList();

    Assert.That(read, Has.Count.EqualTo(2));
    Assert.That(read.Select(w => w.Name), Is.EquivalentTo(new[] { "bolt", "nut" }));
    var bolt = read.Single(w => w.Name == "bolt");
    Assert.That(bolt.Quantity, Is.EqualTo(12));
    Assert.That(bolt.Price, Is.EqualTo(0.25));
  }

  [Test]
  public void GoogleSheets_BuildsItem_WithDeclaredLabel()
  {
    var gateway = new InMemorySheetsGateway();
    var item = ItemFactory.Enumerable.GoogleSheets<Widget>(
      "my-label", SpreadsheetId, TableName, gateway);

    Assert.That(item.Label, Is.EqualTo("my-label"));
  }

  [Test]
  public async Task GoogleSheets_HonorsCustomSaveFunc()
  {
    var gateway = new InMemorySheetsGateway();
    gateway.RegisterSpreadsheet(SpreadsheetId);

    var saveFuncCalled = false;
    var item = ItemFactory.Enumerable.GoogleSheets<Widget>(
      "widgets", SpreadsheetId, TableName, gateway,
      saveFunc: (gw, ssId, table, rows, ct) =>
      {
        saveFuncCalled = true;
        // Compose on top of the default create-if-absent + replace.
        return GoogleSheetsStorageAdapter<Widget>.DefaultSave(gw, ssId, table, rows, ct);
      });

    await Expect(item.Save(new[] { new Widget { Name = "gear", Quantity = 1, Price = 9.99 } }));

    Assert.That(saveFuncCalled, Is.True, "the custom saveFunc should be invoked instead of the default");
    var read = (await Expect(item.Load())).ToList();
    Assert.That(read.Single().Name, Is.EqualTo("gear"));
  }
}
