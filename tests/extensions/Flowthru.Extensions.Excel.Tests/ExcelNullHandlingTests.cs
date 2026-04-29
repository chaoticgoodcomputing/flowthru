using ClosedXML.Excel;
using Flowthru.Core.Abstractions;
using Flowthru.Core.Data.Storage.Format;

namespace Flowthru.Extensions.Excel.Tests;

/// <summary>
/// Tests for <see cref="ExcelFormatSerializer{TRow}"/>'s null handling — empty cells and
/// configurable null sentinels.
/// </summary>
[TestFixture]
[Category("Excel")]
public class ExcelNullHandlingTests
{
  // ── Test schemas ──────────────────────────────────────────────────────────

  public record NullableRow : IFlatSchema, ITextSerializable
  {
    [SerializedLabel("id")]
    public required int Id { get; init; }

    [SerializedLabel("nullable_name")]
    public string? NullableName { get; init; }

    [SerializedLabel("non_nullable_name")]
    public string NonNullableName { get; init; } = string.Empty;

    [SerializedLabel("nullable_value")]
    public int? NullableValue { get; init; }
  }

  // ── Helpers ──────────────────────────────────────────────────────────────

  private static Stream CreateXlsx(string sheetName, string[] headers, IEnumerable<object?[]> rows)
  {
    using var workbook = new XLWorkbook();
    var ws = workbook.Worksheets.Add(sheetName);

    for (int col = 0; col < headers.Length; col++)
      ws.Cell(1, col + 1).Value = headers[col];

    int row = 2;
    foreach (var dataRow in rows)
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
      list.Add(item);
    return list;
  }

  // ── Default null handling ─────────────────────────────────────────────────

  [Test]
  public async Task DefaultBehavior_EmptyCellsBecomeNull_ForNullableProperties()
  {
    var xlsx = CreateXlsx(
      "Sheet1",
      ["id", "nullable_name", "non_nullable_name", "nullable_value"],
      [
        [1, "Alice", "Aldous", 42],
        [2, null, "Bob", null],
      ]
    );

    var serializer = new ExcelFormatSerializer<NullableRow>("Sheet1");
    var rows = await ToList(serializer.DeserializeRows(xlsx));

    Assert.That(rows, Has.Count.EqualTo(2));
    Assert.That(rows[1].NullableName, Is.Null);
    Assert.That(rows[1].NullableValue, Is.Null);
  }

  // ── Custom null-value list ────────────────────────────────────────────────

  [Test]
  public async Task CustomNullValues_PandasStyleSentinels_DeserializeToNull()
  {
    var xlsx = CreateXlsx(
      "Sheet1",
      ["id", "nullable_name", "non_nullable_name", "nullable_value"],
      [
        [1, "NA", "Alice", null],
        [2, "N/A", "Bob", null],
        [3, "Charlie", "Charles", 7],
      ]
    );

    var serializer = new ExcelFormatSerializer<NullableRow>(
      "Sheet1",
      nullValues: new[] { string.Empty, "NA", "N/A", "NULL" }
    );
    var rows = await ToList(serializer.DeserializeRows(xlsx));

    Assert.That(rows, Has.Count.EqualTo(3));
    Assert.That(rows[0].NullableName, Is.Null, "'NA' should be treated as null");
    Assert.That(rows[1].NullableName, Is.Null, "'N/A' should be treated as null");
    Assert.That(rows[2].NullableName, Is.EqualTo("Charlie"));
  }

  [Test]
  public async Task CustomNullValues_NonNullableStringUnaffected()
  {
    var xlsx = CreateXlsx(
      "Sheet1",
      ["id", "nullable_name", "non_nullable_name", "nullable_value"],
      [
        // Non-nullable string with the "NA" sentinel — should stay as "NA", not become
        // null. The kit-level convention is that nullability annotations on the property
        // gate the override; non-nullable properties never see the override.
        [1, "Alice", "NA", 42],
      ]
    );

    var serializer = new ExcelFormatSerializer<NullableRow>(
      "Sheet1",
      nullValues: new[] { string.Empty, "NA" }
    );
    var rows = await ToList(serializer.DeserializeRows(xlsx));

    Assert.That(rows[0].NonNullableName, Is.EqualTo("NA"));
  }
}
