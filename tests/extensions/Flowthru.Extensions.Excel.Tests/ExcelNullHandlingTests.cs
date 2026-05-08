using ClosedXML.Excel;
using Flowthru.Data.Storage.Excel;
using Flowthru.Extensions.Excel.Tests.Fixtures;

namespace Flowthru.Extensions.Excel.Tests;

/// <summary>
/// Null-handling for <see cref="ExcelFormatSerializer{TRow}"/> —
/// blank cells, configurable null sentinels, and non-nullable-string
/// preservation.
/// </summary>
[TestFixture]
[Category("Excel")]
public class ExcelNullHandlingTests
{
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
        if (dataRow[col] is null)
        {
          // Leave the cell blank (DBNull from ExcelDataReader's perspective).
          continue;
        }
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

  [Test]
  public async Task EmptyCells_DeserializeAsNull_ForNullableProperties()
  {
    using var xlsx = CreateXlsx(
      "Data",
      ["Id", "OptionalName", "OptionalCount"],
      [
        [1, "Alice", 7],
        [2, null, null],
      ]
    );

    var serializer = new ExcelFormatSerializer<NullableProductRow>("Data");
    var rows = await ToList(serializer.DeserializeRows(xlsx));

    Assert.That(rows, Has.Count.EqualTo(2));
    Assert.That(rows[0].OptionalName, Is.EqualTo("Alice"));
    Assert.That(rows[0].OptionalCount, Is.EqualTo(7));
    Assert.That(rows[1].OptionalName, Is.Null,
      "Empty cell should deserialize to null for string?.");
    Assert.That(rows[1].OptionalCount, Is.Null,
      "Empty cell should deserialize to null for int?.");
  }

  [Test]
  public async Task CustomNullValues_StringSentinels_DeserializeAsNull()
  {
    using var xlsx = CreateXlsx(
      "Data",
      ["Id", "OptionalName", "OptionalCount"],
      [
        [1, "NA", 5],
        [2, "Charlie", 7],
      ]
    );

    var serializer = new ExcelFormatSerializer<NullableProductRow>(
      "Data", nullValues: new[] { string.Empty, "NA", "N/A" }
    );
    var rows = await ToList(serializer.DeserializeRows(xlsx));

    Assert.That(rows, Has.Count.EqualTo(2));
    Assert.That(rows[0].OptionalName, Is.Null,
      "'NA' should be treated as null when configured as a sentinel.");
    Assert.That(rows[1].OptionalName, Is.EqualTo("Charlie"));
    Assert.That(rows[1].OptionalCount, Is.EqualTo(7));
  }
}
