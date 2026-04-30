using System.Globalization;
using CsvHelper.Configuration;
using Flowthru.Core.Data.Storage.Format;
using Flowthru.Tests.Kits.Schemas;

namespace Flowthru.Extensions.Csv.Tests;

/// <summary>
/// Direct accessor tests for <see cref="CsvFormatSerializer{TRow}"/> — covers the
/// custom-config ctor and the public property surface that the conformance kit doesn't
/// otherwise exercise.
/// </summary>
[TestFixture]
public class CsvFormatSerializerAccessorTests
{
  [Test]
  public void Ctor_WithCustomCsvConfiguration_AppliesConfiguration()
  {
    var config = new CsvConfiguration(CultureInfo.InvariantCulture, typeof(TraditionalSchema))
    {
      HasHeaderRecord = false,
      Delimiter = ";",
    };

    var serializer = new CsvFormatSerializer<TraditionalSchema>(config);

    Assert.That(serializer.Configuration, Is.SameAs(config));
    Assert.That(serializer.Configuration.HasHeaderRecord, Is.False);
    Assert.That(serializer.Configuration.Delimiter, Is.EqualTo(";"));
  }

  [Test]
  public void Configuration_DefaultCtor_ReturnsConfiguredValue()
  {
    var serializer = new CsvFormatSerializer<TraditionalSchema>();

    Assert.That(serializer.Configuration, Is.Not.Null);
    Assert.That(serializer.Configuration.HasHeaderRecord, Is.True);
  }

  [Test]
  public void NullValues_DefaultCtor_ContainsExpectedSentinels()
  {
    var serializer = new CsvFormatSerializer<TraditionalSchema>();

    Assert.That(serializer.NullValues, Is.Not.Null.And.Not.Empty);
    Assert.That(serializer.NullValues, Has.Some.EqualTo(""));
  }

  [Test]
  public void NullValues_CustomList_PreservesOrder()
  {
    var custom = new[] { "", "NA", "N/A", "NULL" };
    var serializer = new CsvFormatSerializer<TraditionalSchema>(custom);

    Assert.That(serializer.NullValues, Is.EqualTo(custom));
  }

  [Test]
  public void Ctor_NullConfiguration_Throws()
  {
    Assert.That(
      () => new CsvFormatSerializer<TraditionalSchema>((CsvConfiguration)null!),
      Throws.ArgumentNullException
    );
  }
}
