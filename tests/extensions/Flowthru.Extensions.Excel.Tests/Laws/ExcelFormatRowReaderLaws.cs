using ClosedXML.Excel;
using Flowthru.Data.Storage;
using Flowthru.Data.Storage.Excel;
using Flowthru.Extensions.Excel.Tests.Fixtures;
using Flowthru.Tests.Kits.Storage;

namespace Flowthru.Extensions.Excel.Tests.Laws;

/// <summary>
/// <see cref="IFormatRowReaderLaws{TRow}"/> binding for
/// <see cref="ExcelFormatSerializer{TRow}"/> over <see cref="ProductRow"/>.
/// Asserts the read-only structural contract, the trait/marker drift
/// law, and that the reader yields the rows the ClosedXML-built
/// fixture encodes.
/// </summary>
[TestFixture]
[Category("Excel")]
public class ExcelFormatRowReaderLaws_ProductRow : IFormatRowReaderLaws<ProductRow>
{
  private const string SheetName = "Products";

  protected override IFormatRowReader<ProductRow> CreateReader() =>
    new ExcelFormatSerializer<ProductRow>(SheetName);

  protected override IEnumerable<ProductRow> ExpectedRows => new[]
  {
    new ProductRow { Id = 1, Name = "Widget", Price = 9.99 },
    new ProductRow { Id = 2, Name = "Gadget", Price = 19.99 },
    new ProductRow { Id = 3, Name = "Gizmo", Price = 29.99 },
  };

  protected override Stream CreateFixtureStream()
  {
    using var workbook = new XLWorkbook();
    var ws = workbook.Worksheets.Add(SheetName);

    ws.Cell(1, 1).Value = "Id";
    ws.Cell(1, 2).Value = "Name";
    ws.Cell(1, 3).Value = "Price";

    int row = 2;
    foreach (var product in ExpectedRows)
    {
      ws.Cell(row, 1).Value = product.Id;
      ws.Cell(row, 2).Value = product.Name;
      ws.Cell(row, 3).Value = product.Price;
      row++;
    }

    var stream = new MemoryStream();
    workbook.SaveAs(stream);
    stream.Position = 0;
    return stream;
  }
}

/// <summary>
/// <see cref="IFormatRowReaderLaws{TRow}"/> binding exercising the
/// planner-emitted <c>[SerializedEnum]</c> mappings end-to-end on the
/// Excel read path.
/// </summary>
[TestFixture]
[Category("Excel")]
public class ExcelFormatRowReaderLaws_CheckStatusRow : IFormatRowReaderLaws<CheckStatusRow>
{
  private const string SheetName = "Status";

  private static readonly Guid Id1 = Guid.NewGuid();
  private static readonly Guid Id2 = Guid.NewGuid();

  protected override IFormatRowReader<CheckStatusRow> CreateReader() =>
    new ExcelFormatSerializer<CheckStatusRow>(SheetName);

  protected override IEnumerable<CheckStatusRow> ExpectedRows => new[]
  {
    new CheckStatusRow { Id = Id1, Status = CheckStatus.Complete },
    new CheckStatusRow { Id = Id2, Status = CheckStatus.Incomplete },
  };

  protected override Stream CreateFixtureStream()
  {
    using var workbook = new XLWorkbook();
    var ws = workbook.Worksheets.Add(SheetName);

    ws.Cell(1, 1).Value = "Id";
    ws.Cell(1, 2).Value = "Status";

    ws.Cell(2, 1).Value = Id1.ToString();
    ws.Cell(2, 2).Value = "t";
    ws.Cell(3, 1).Value = Id2.ToString();
    ws.Cell(3, 2).Value = "f";

    var stream = new MemoryStream();
    workbook.SaveAs(stream);
    stream.Position = 0;
    return stream;
  }
}
