using ClosedXML.Excel;
using Flowthru.Data.Storage;
using Flowthru.Data.Storage.Excel;
using Flowthru.Extensions.Excel.Tests.Fixtures;

namespace Flowthru.Extensions.Excel.Tests;

/// <summary>
/// Direct exercises of <see cref="ExcelFormatSerializer{TRow}"/> on
/// flat schemas — sheet selection, <c>[SerializedLabel]</c> honoring,
/// non-seekable streams, and the schema-mismatch translation when
/// the requested sheet is missing.
/// </summary>
[TestFixture]
[Category("Excel")]
public class ExcelFormatSerializerTests
{
  // ── Helpers ──────────────────────────────────────────────────────────

  /// <summary>
  /// Build an in-memory .xlsx via ClosedXML — the writer-side companion
  /// to ExcelDataReader, used to seed test fixtures.
  /// </summary>
  private static Stream CreateXlsx(
    string sheetName,
    string[] headers,
    IEnumerable<object?[]> dataRows
  )
  {
    using var workbook = new XLWorkbook();
    var ws = workbook.Worksheets.Add(sheetName);

    for (int col = 0; col < headers.Length; col++)
    {
      ws.Cell(1, col + 1).Value = headers[col];
    }

    int row = 2;
    foreach (var dataRow in dataRows)
    {
      for (int col = 0; col < dataRow.Length; col++)
      {
        ws.Cell(row, col + 1).Value = XLCellValue.FromObject(dataRow[col]);
      }
      row++;
    }

    var stream = new MemoryStream();
    workbook.SaveAs(stream);
    stream.Position = 0;
    return stream;
  }

  private static async Task<List<T>> ToList<T>(IAsyncEnumerable<T> source)
  {
    var list = new List<T>();
    await foreach (var item in source)
    {
      list.Add(item);
    }
    return list;
  }

  // ── Traits / structural read-only-ness ──────────────────────────────

  [Test]
  public void Traits_CanWrite_IsFalse()
  {
    Assert.That(new ExcelFormatSerializer<ProductRow>("Sheet1").Traits.CanWrite, Is.False);
  }

  [Test]
  public void Type_DoesNotImplementWriterSegment()
  {
    Assert.That(
      typeof(ExcelFormatSerializer<ProductRow>)
        .GetInterfaces()
        .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IFormatRowWriter<>)),
      Is.False,
      "Excel is read-only by construction; structural read-only-ness depends on the "
        + "writer segment being absent from the type."
    );
  }

  // ── DeserializeRows — happy path ────────────────────────────────────

  [Test]
  public async Task DeserializeRows_ReturnsAllDataRows()
  {
    using var xlsx = CreateXlsx(
      "Products",
      ["Id", "Name", "Price"],
      [
        [1, "Widget", 9.99],
        [2, "Gadget", 19.99],
      ]
    );
    var serializer = new ExcelFormatSerializer<ProductRow>("Products");

    var result = await ToList(serializer.DeserializeRows(xlsx));

    Assert.That(result, Has.Count.EqualTo(2));
    Assert.That(result[0].Id, Is.EqualTo(1));
    Assert.That(result[0].Name, Is.EqualTo("Widget"));
    Assert.That(result[0].Price, Is.EqualTo(9.99).Within(0.001));
    Assert.That(result[1].Id, Is.EqualTo(2));
  }

  [Test]
  public async Task DeserializeRows_EmptySheet_ReturnsNoRows()
  {
    using var xlsx = CreateXlsx("Empty", ["Id", "Name", "Price"], []);
    var serializer = new ExcelFormatSerializer<ProductRow>("Empty");

    var result = await ToList(serializer.DeserializeRows(xlsx));

    Assert.That(result, Is.Empty);
  }

  // ── SerializedLabel ─────────────────────────────────────────────────

  [Test]
  public async Task SerializedLabel_MapsExternalColumnNamesToProperties()
  {
    using var xlsx = CreateXlsx(
      "Data",
      ["product_id", "product_name"],
      [[42, "Acme Widget"]]
    );
    var serializer = new ExcelFormatSerializer<LabeledProductRow>("Data");

    var result = await ToList(serializer.DeserializeRows(xlsx));

    Assert.That(result, Has.Count.EqualTo(1));
    Assert.That(result[0].ProductId, Is.EqualTo(42));
    Assert.That(result[0].ProductName, Is.EqualTo("Acme Widget"));
  }

  // ── Sheet selection ─────────────────────────────────────────────────

  [Test]
  public async Task DeserializeRows_ReadsFromNamedSheet_IgnoresOthers()
  {
    using var workbook = new XLWorkbook();
    var ws1 = workbook.Worksheets.Add("Decoy");
    ws1.Cell(1, 1).Value = "Id";
    ws1.Cell(1, 2).Value = "Name";
    ws1.Cell(1, 3).Value = "Price";
    ws1.Cell(2, 1).Value = 99;
    ws1.Cell(2, 2).Value = "Decoy";
    ws1.Cell(2, 3).Value = 0.0;

    var ws2 = workbook.Worksheets.Add("Target");
    ws2.Cell(1, 1).Value = "Id";
    ws2.Cell(1, 2).Value = "Name";
    ws2.Cell(1, 3).Value = "Price";
    ws2.Cell(2, 1).Value = 7;
    ws2.Cell(2, 2).Value = "Correct";
    ws2.Cell(2, 3).Value = 3.14;

    using var stream = new MemoryStream();
    workbook.SaveAs(stream);
    stream.Position = 0;

    var serializer = new ExcelFormatSerializer<ProductRow>("Target");
    var result = await ToList(serializer.DeserializeRows(stream));

    Assert.That(result, Has.Count.EqualTo(1));
    Assert.That(result[0].Name, Is.EqualTo("Correct"));
  }

  [Test]
  public void DeserializeRows_SheetNotFound_ThrowsSchemaMismatchException()
  {
    var xlsx = CreateXlsx(
      "Sheet1",
      ["Id", "Name", "Price"],
      [[1, "X", 1.0]]
    );
    var serializer = new ExcelFormatSerializer<ProductRow>("NonExistent");

    Assert.ThrowsAsync<SchemaMismatchException>(
      async () => await ToList(serializer.DeserializeRows(xlsx)),
      "A missing sheet is a structural mismatch — must surface as "
        + "SchemaMismatchException so the composed adapter's boundary lifts it to "
        + "typed RuntimeError.SchemaMismatch / ValidationErrorType.SchemaMismatch."
    );
  }

  // Non-seekable-stream coverage now lives in IFormatRowReaderLaws.NonSeekableDeserializeLaw,
  // inherited by ExcelFormatRowReaderLaws — the contract is asserted once for every
  // read-only format rather than ad-hoc per format here.

  // ── SerializedEnum ──────────────────────────────────────────────────

  [Test]
  public async Task SerializedEnum_DecodesViaPlannerEmittedMappings()
  {
    var id1 = Guid.NewGuid();
    var id2 = Guid.NewGuid();
    using var xlsx = CreateXlsx(
      "Status",
      ["Id", "Status"],
      [[id1.ToString(), "t"], [id2.ToString(), "f"]]
    );

    var serializer = new ExcelFormatSerializer<CheckStatusRow>("Status");
    var rows = await ToList(serializer.DeserializeRows(xlsx));

    Assert.That(rows, Has.Count.EqualTo(2));
    Assert.That(rows[0].Status, Is.EqualTo(CheckStatus.Complete));
    Assert.That(rows[1].Status, Is.EqualTo(CheckStatus.Incomplete));
  }
}
