using ClosedXML.Excel;
using Flowthru.Core.Abstractions;
using Flowthru.Core.Data.Storage;
using Flowthru.Core.Data.Storage.Format;

namespace Flowthru.Extensions.Excel.Tests;

/// <summary>
/// Tests for <see cref="ExcelFormatSerializer{TRow}"/>.
///
/// Error-surface focus (from CONTRIBUTING.md):
/// <list type="bullet">
/// <item>Write attempts fail fast with <see cref="NotSupportedException"/> — build-time
///   trait (<c>CanWrite=false</c>) surfaces as a pre-flight check.</item>
/// <item>Missing sheet fails fast with <see cref="InvalidOperationException"/> before
///   any row data is processed.</item>
/// <item>Non-seekable streams are buffered transparently — the caller never has to
///   manage this.</item>
/// </list>
/// </summary>
[TestFixture]
[Category("Excel")]
public class ExcelFormatSerializerTests
{
  // ── Fixture types ─────────────────────────────────────────────────────────

  private class ProductRow : IFlatSchema, ITextSerializable
  {
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public double Price { get; set; }
  }

  private class LabeledRow : IFlatSchema, ITextSerializable
  {
    [SerializedLabel("product_id")]
    public int ProductId { get; set; }

    [SerializedLabel("product_name")]
    public string ProductName { get; set; } = "";
  }

  // ── Helpers ───────────────────────────────────────────────────────────────

  /// <summary>
  /// Creates an in-memory .xlsx stream using ClosedXML with the given sheet name and rows.
  /// The first row is a header row derived from the provided column names.
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
      ws.Cell(1, col + 1).Value = headers[col];

    int row = 2;
    foreach (var dataRow in dataRows)
    {
      for (int col = 0; col < dataRow.Length; col++)
        ws.Cell(row, col + 1).Value = XLCellValue.FromObject(dataRow[col]);
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
      list.Add(item);
    return list;
  }

  // ── Traits ────────────────────────────────────────────────────────────────

  [Test]
  public void Traits_CanWrite_IsFalse()
  {
    Assert.That(new ExcelFormatSerializer<ProductRow>("Sheet1").Traits.CanWrite, Is.False);
  }

  // ── Structural read-only-ness ─────────────────────────────────────────────
  // Excel does not implement IFormatRowWriter<TRow> at all — read-only-ness is a
  // compile-time signal carried by the type, not a runtime exception. Phase D
  // (capability-segmented interfaces) replaced the throw-from-SerializeRows pattern
  // with the absence of the writer segment.

  [Test]
  public void Type_DoesNotImplementWriterSegment()
  {
    Assert.That(
      typeof(ExcelFormatSerializer<ProductRow>)
        .GetInterfaces()
        .Any(i => i.IsGenericType
          && i.GetGenericTypeDefinition() == typeof(IFormatRowWriter<>)),
      Is.False,
      "ExcelFormatSerializer<TRow> must not implement IFormatRowWriter<TRow>; "
        + "structural read-only-ness depends on the writer segment being absent."
    );
  }

  // ── DeserializeRows — happy path ──────────────────────────────────────────

  [Test]
  public async Task DeserializeRows_ReturnsAllDataRows()
  {
    var xlsx = CreateXlsx(
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
    var xlsx = CreateXlsx("Empty", ["Id", "Name", "Price"], []);
    var serializer = new ExcelFormatSerializer<ProductRow>("Empty");

    var result = await ToList(serializer.DeserializeRows(xlsx));

    Assert.That(result, Is.Empty);
  }

  // ── SerializedLabel ───────────────────────────────────────────────────────

  [Test]
  public async Task SerializedLabel_MapsExternalColumnNamesToProperties()
  {
    var xlsx = CreateXlsx(
      "Data",
      ["product_id", "product_name"],
      [
        [42, "Acme Widget"],
      ]
    );
    var serializer = new ExcelFormatSerializer<LabeledRow>("Data");

    var result = await ToList(serializer.DeserializeRows(xlsx));

    Assert.That(result, Has.Count.EqualTo(1));
    Assert.That(result[0].ProductId, Is.EqualTo(42));
    Assert.That(result[0].ProductName, Is.EqualTo("Acme Widget"));
  }

  // ── Sheet selection ───────────────────────────────────────────────────────

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

    var stream = new MemoryStream();
    workbook.SaveAs(stream);
    stream.Position = 0;

    var serializer = new ExcelFormatSerializer<ProductRow>("Target");
    var result = await ToList(serializer.DeserializeRows(stream));

    Assert.That(result, Has.Count.EqualTo(1));
    Assert.That(result[0].Name, Is.EqualTo("Correct"));
  }

  [Test]
  public void DeserializeRows_SheetNotFound_ThrowsInvalidOperationException()
  {
    var xlsx = CreateXlsx(
      "Sheet1",
      ["Id", "Name", "Price"],
      [
        [1, "X", 1.0],
      ]
    );
    var serializer = new ExcelFormatSerializer<ProductRow>("NonExistent");

    Assert.ThrowsAsync<InvalidOperationException>(
      async () => await ToList(serializer.DeserializeRows(xlsx))
    );
  }

  // ── Non-seekable stream ───────────────────────────────────────────────────

  [Test]
  public async Task DeserializeRows_NonSeekableStream_BuffersAndReadsSuccessfully()
  {
    var xlsx = CreateXlsx(
      "Products",
      ["Id", "Name", "Price"],
      [
        [5, "Seekless", 1.23],
      ]
    );

    // Wrap in a stream that disables seeking.
    var nonSeekable = new NonSeekableStream(xlsx);
    Assert.That(nonSeekable.CanSeek, Is.False, "Precondition: stream must be non-seekable");

    var serializer = new ExcelFormatSerializer<ProductRow>("Products");
    var result = await ToList(serializer.DeserializeRows(nonSeekable));

    Assert.That(result, Has.Count.EqualTo(1));
    Assert.That(result[0].Id, Is.EqualTo(5));
  }

  // ── Helper: non-seekable stream wrapper ──────────────────────────────────

  private sealed class NonSeekableStream(Stream inner) : Stream
  {
    public override bool CanRead => inner.CanRead;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();
    public override long Position
    {
      get => throw new NotSupportedException();
      set => throw new NotSupportedException();
    }

    public override void Flush() => inner.Flush();

    public override int Read(byte[] buffer, int offset, int count) =>
      inner.Read(buffer, offset, count);

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) =>
      throw new NotSupportedException();
  }
}
