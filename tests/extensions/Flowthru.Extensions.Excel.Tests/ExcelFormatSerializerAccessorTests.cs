using Flowthru.Data.Storage.Excel;
using Flowthru.Extensions.Excel.Tests.Fixtures;

namespace Flowthru.Extensions.Excel.Tests;

/// <summary>
/// Direct accessors on <see cref="ExcelFormatSerializer{TRow}"/> —
/// the public property surface and constructor null-argument guards.
/// </summary>
[TestFixture]
[Category("Excel")]
public class ExcelFormatSerializerAccessorTests
{
  [Test]
  public void Ctor_ExposesSheetName()
  {
    var serializer = new ExcelFormatSerializer<ProductRow>("Reports");
    Assert.That(serializer.SheetName, Is.EqualTo("Reports"));
  }

  [Test]
  public void NullValues_DefaultCtor_ContainsExpectedSentinels()
  {
    var serializer = new ExcelFormatSerializer<ProductRow>("Sheet1");

    Assert.That(serializer.NullValues, Is.Not.Null.And.Not.Empty);
    Assert.That(serializer.NullValues, Has.Some.EqualTo(""));
  }

  [Test]
  public void NullValues_CustomList_PreservesOrder()
  {
    var custom = new[] { "", "NA", "N/A", "NULL" };
    var serializer = new ExcelFormatSerializer<ProductRow>("Sheet1", custom);
    Assert.That(serializer.NullValues, Is.EqualTo(custom));
  }

  [Test]
  public void Ctor_NullSheetName_Throws()
  {
    Assert.That(
      () => new ExcelFormatSerializer<ProductRow>(null!),
      Throws.ArgumentNullException
    );
  }

  [Test]
  public void Ctor_NullNullValues_Throws()
  {
    Assert.That(
      () => new ExcelFormatSerializer<ProductRow>("Sheet1", null!),
      Throws.ArgumentNullException
    );
  }
}
