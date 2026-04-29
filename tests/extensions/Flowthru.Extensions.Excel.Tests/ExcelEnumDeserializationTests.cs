using ClosedXML.Excel;
using Flowthru.Core.Data.Storage.Format;
using Flowthru.Tests.Kits.Schemas;

namespace Flowthru.Extensions.Excel.Tests;

/// <summary>
/// Tests that <see cref="ExcelFormatSerializer{TRow}"/> deserializes <c>[SerializedEnum]</c>
/// fields by routing through <c>EnumSerializationHelper.ParseEnumFromString</c>. Excel is
/// read-only at the format level, so the conformance kit's round-trip cannot exercise this
/// path; this fixture seeds the .xlsx via ClosedXML and verifies the read side.
/// </summary>
/// <remarks>
/// Closes the residual coverage entry from the coverage audit's final breakdown:
/// <c>EnumSerializationHelper.ParseEnumFromString</c> was reachable only through
/// <c>ExcelFormatSerializer.DeserializeRows</c>'s enum-handling branch and had no test
/// coverage prior to Phase C.
/// </remarks>
[TestFixture]
[Category("Excel")]
public class ExcelEnumDeserializationTests
{
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

  [Test]
  public async Task DeserializeRows_SerializedEnumValues_RoutesThroughEnumHelper()
  {
    var id1 = Guid.NewGuid();
    var id2 = Guid.NewGuid();

    // ClosedXML writes ["t", "f"] as raw cell strings; ExcelFormatSerializer.DeserializeRows
    // sees those strings, sees that the property type is the enum CheckStatus, and dispatches
    // to EnumSerializationHelper.ParseEnumFromString, which honors the [SerializedEnum]
    // attribute mapping.
    var xlsx = CreateXlsx(
      "Status",
      ["id", "status"],
      [[id1.ToString(), "t"], [id2.ToString(), "f"]]
    );

    var serializer = new ExcelFormatSerializer<CheckStatusSchema>("Status");
    var rows = await ToList(serializer.DeserializeRows(xlsx));

    Assert.That(rows, Has.Count.EqualTo(2));
    Assert.That(rows[0].Status, Is.EqualTo(CheckStatus.Complete));
    Assert.That(rows[1].Status, Is.EqualTo(CheckStatus.Incomplete));
  }
}
