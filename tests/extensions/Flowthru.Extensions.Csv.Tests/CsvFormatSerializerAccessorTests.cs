using System.Globalization;
using CsvHelper.Configuration;
using Flowthru.Extensions.Csv.Tests.Fixtures;
using Flowthru.Data.Storage.Csv;

namespace Flowthru.Extensions.Csv.Tests;

/// <summary>
/// Direct accessors on <see cref="CsvFormatSerializer{TRow}"/>: the
/// custom-config constructor and the public property surface.
/// </summary>
[TestFixture]
[Category("Csv")]
public class CsvFormatSerializerAccessorTests
{
  [Test]
  public void Ctor_WithCustomCsvConfiguration_AppliesConfiguration()
  {
    var config = new CsvConfiguration(CultureInfo.InvariantCulture)
    {
      HasHeaderRecord = false,
      Delimiter = ";",
    };

    var serializer = new CsvFormatSerializer<FlatRow>(config);

    Assert.That(serializer.Configuration, Is.SameAs(config));
    Assert.That(serializer.Configuration.HasHeaderRecord, Is.False);
    Assert.That(serializer.Configuration.Delimiter, Is.EqualTo(";"));
  }

  [Test]
  public void Configuration_DefaultCtor_ReturnsConfiguredValue()
  {
    var serializer = new CsvFormatSerializer<FlatRow>();

    Assert.That(serializer.Configuration, Is.Not.Null);
    Assert.That(serializer.Configuration.HasHeaderRecord, Is.True);
  }

  [Test]
  public void NullValues_DefaultCtor_ContainsExpectedSentinels()
  {
    var serializer = new CsvFormatSerializer<FlatRow>();

    Assert.That(serializer.NullValues, Is.Not.Null.And.Not.Empty);
    Assert.That(serializer.NullValues, Has.Some.EqualTo(""));
  }

  [Test]
  public void NullValues_CustomList_PreservesOrder()
  {
    var custom = new[] { "", "NA", "N/A", "NULL" };
    var serializer = new CsvFormatSerializer<FlatRow>(custom);

    Assert.That(serializer.NullValues, Is.EqualTo(custom));
  }

  [Test]
  public void Ctor_NullConfiguration_Throws()
  {
    Assert.That(
      () => new CsvFormatSerializer<FlatRow>((CsvConfiguration)null!),
      Throws.ArgumentNullException
    );
  }

  [Test]
  public void Ctor_NullNullValues_Throws()
  {
    Assert.That(
      () => new CsvFormatSerializer<FlatRow>((IReadOnlyList<string>)null!),
      Throws.ArgumentNullException
    );
  }
}
