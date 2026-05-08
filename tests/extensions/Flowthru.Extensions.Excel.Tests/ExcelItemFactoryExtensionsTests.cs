using ClosedXML.Excel;
using Flowthru.Data.Catalog;
using Flowthru.Data.Storage;
using Flowthru.Extensions.Excel.Tests.Fixtures;
using Flowthru.Prelude;
using Flowthru.Validation.Runtime;
using SysIO = System.IO;

namespace Flowthru.Extensions.Excel.Tests;

/// <summary>
/// Smart-constructor smoke tests for the
/// <c>CsvItemFactoryExtensions</c>-equivalent Excel extension methods —
/// verifies <c>ItemFactory.Enumerable.Excel&lt;T&gt;(...)</c> and
/// <c>ItemFactory.Directory.Excel&lt;T&gt;(...)</c> wire up the
/// composed adapter correctly and that the read-only contract holds
/// at the storage-traits level.
/// </summary>
[TestFixture]
[Category("Excel")]
public class ExcelItemFactoryExtensionsTests
{
  private string _root = null!;

  [SetUp]
  public void SetUp()
  {
    _root = SysIO.Path.Combine(SysIO.Path.GetTempPath(), $"flowthru-excel-ife-{Guid.NewGuid():N}");
    SysIO.Directory.CreateDirectory(_root);
  }

  [TearDown]
  public void TearDown()
  {
    if (SysIO.Directory.Exists(_root))
    {
      try { SysIO.Directory.Delete(_root, recursive: true); }
      catch { /* best effort */ }
    }
  }

  private static void WriteXlsx(string path, string sheetName, string[] headers, IEnumerable<object?[]> rows)
  {
    using var workbook = new XLWorkbook();
    var ws = workbook.Worksheets.Add(sheetName);
    for (int col = 0; col < headers.Length; col++)
    {
      ws.Cell(1, col + 1).Value = headers[col];
    }
    int row = 2;
    foreach (var dataRow in rows)
    {
      for (int col = 0; col < dataRow.Length; col++)
      {
        ws.Cell(row, col + 1).Value = XLCellValue.FromObject(dataRow[col]);
      }
      row++;
    }
    workbook.SaveAs(path);
  }

  // ── Enumerable.Excel<T> ──────────────────────────────────────────────

  [Test]
  public async Task EnumerableExcel_LoadsThroughComposedAdapter()
  {
    var path = SysIO.Path.Combine(_root, "products.xlsx");
    WriteXlsx(path, "Products", ["Id", "Name", "Price"], [
      [1, "Widget", 9.99],
      [2, "Gadget", 19.99],
    ]);

    var item = ItemFactory.Enumerable.Excel<ProductRow>("products", path, "Products");

    var loadResult = await item.Load().Run();
    var rows = ((EffResult<IEnumerable<ProductRow>>.Success)loadResult).Value.ToList();

    Assert.That(rows, Has.Count.EqualTo(2));
    Assert.That(rows[0].Name, Is.EqualTo("Widget"));
    Assert.That(rows[1].Name, Is.EqualTo("Gadget"));
  }

  [Test]
  public void EnumerableExcel_BuildsItemWithExpectedLabel()
  {
    var item = ItemFactory.Enumerable.Excel<ProductRow>("products", "ignored.xlsx", "Sheet1");
    Assert.That(item.Label, Is.EqualTo("products"));
  }

  [Test]
  public async Task EnumerableExcel_SaveReportsFailureBecauseFormatIsReadOnly()
  {
    var path = SysIO.Path.Combine(_root, "products.xlsx");
    var item = ItemFactory.Enumerable.Excel<ProductRow>("products", path, "Sheet1");

    var saveResult = await item.Save(new[]
    {
      new ProductRow { Id = 1, Name = "x", Price = 1.0 },
    }).Run();

    Assert.That(saveResult, Is.InstanceOf<EffResult<FlowUnit>.Failure>(),
      "Excel format is structurally read-only — Save must fail fast at the "
        + "composed adapter's CanWrite gate, not silently succeed."
    );
  }

  [Test]
  public async Task EnumerableExcel_SheetNotFound_SurfacesTypedSchemaMismatch()
  {
    var path = SysIO.Path.Combine(_root, "wrong-sheet.xlsx");
    WriteXlsx(path, "DifferentSheet", ["Id", "Name", "Price"], [[1, "X", 1.0]]);

    var item = ItemFactory.Enumerable.Excel<ProductRow>("products", path, "Products");
    var loadResult = await item.Load().Run();

    Assert.That(loadResult, Is.InstanceOf<EffResult<IEnumerable<ProductRow>>.Failure>());
    var failure = (EffResult<IEnumerable<ProductRow>>.Failure)loadResult;
    Assert.That(failure.Error, Is.InstanceOf<RuntimeError.SchemaMismatch>(),
      "Missing sheet must surface as the typed RuntimeError.SchemaMismatch — "
        + "ExcelFormatSerializer throws SchemaMismatchException; the composed "
        + "adapter's MapError lifts it to the typed variant."
    );
  }

  // ── Directory.Excel<T> ───────────────────────────────────────────────

  [Test]
  public async Task DirectoryExcel_LoadsOneFilePerEntry()
  {
    WriteXlsx(SysIO.Path.Combine(_root, "a.xlsx"), "Products", ["Id", "Name", "Price"],
      [[1, "Alice", 1.5]]);
    WriteXlsx(SysIO.Path.Combine(_root, "b.xlsx"), "Products", ["Id", "Name", "Price"],
      [[2, "Bob", 2.5]]);

    var item = ItemFactory.Directory.Excel<ProductRow>("dir", _root, "Products");
    var loadResult = await item.Load().Run();
    var loaded = ((EffResult<Directory<IEnumerable<ProductRow>>>.Success)loadResult).Value;

    Assert.That(loaded.Count, Is.EqualTo(2));
    var byBaseName = loaded.ToDictionary(
      kvp => SysIO.Path.GetFileName(kvp.Key),
      kvp => kvp.Value.Single().Name
    );
    Assert.That(byBaseName["a.xlsx"], Is.EqualTo("Alice"));
    Assert.That(byBaseName["b.xlsx"], Is.EqualTo("Bob"));
  }

  [Test]
  public void DirectoryExcel_BuildsItemWithExpectedLabel()
  {
    var item = ItemFactory.Directory.Excel<ProductRow>("shuttles", _root, "Sheet1");
    Assert.That(item.Label, Is.EqualTo("shuttles"));
  }
}
