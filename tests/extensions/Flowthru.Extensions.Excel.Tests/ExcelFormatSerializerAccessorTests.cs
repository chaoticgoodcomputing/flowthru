using Flowthru.Core.Data.Storage.Format;
using Flowthru.Tests.Kits.Schemas;

namespace Flowthru.Extensions.Excel.Tests;

/// <summary>
/// Direct accessor tests for <see cref="ExcelFormatSerializer{TRow}"/>.
/// </summary>
[TestFixture]
public class ExcelFormatSerializerAccessorTests
{
  [Test]
  public void NullValues_DefaultCtor_ContainsEmptyString()
  {
    var serializer = new ExcelFormatSerializer<TraditionalSchema>(sheetName: "Sheet1");

    Assert.That(serializer.NullValues, Is.Not.Null);
    Assert.That(serializer.NullValues, Has.Some.EqualTo(""));
  }

  [Test]
  public void NullValues_CustomList_PreservesOrder()
  {
    var custom = new[] { "", "NA", "N/A", "NULL" };
    var serializer = new ExcelFormatSerializer<TraditionalSchema>(
      sheetName: "Sheet1",
      nullValues: custom
    );

    Assert.That(serializer.NullValues, Is.EqualTo(custom));
  }
}
