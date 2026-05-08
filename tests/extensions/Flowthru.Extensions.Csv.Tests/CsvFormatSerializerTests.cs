using System.Text;
using Flowthru.Extensions.Csv.Tests.Fixtures;
using Flowthru.Data.Storage.Csv;

namespace Flowthru.Extensions.Csv.Tests;

/// <summary>
/// Direct exercises of <see cref="CsvFormatSerializer{TRow}"/> on flat
/// schemas — round-trip, header semantics, <c>[SerializedLabel]</c>
/// honoring, and null-argument guards.
/// </summary>
[TestFixture]
[Category("Csv")]
public class CsvFormatSerializerTests
{
  private static async IAsyncEnumerable<T> AsAsync<T>(IEnumerable<T> source)
  {
    foreach (var item in source)
    {
      yield return item;
      await Task.Yield();
    }
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

  // ── Round-trip ──────────────────────────────────────────────────────

  [Test]
  public async Task RoundTrip_SerializeThenDeserialize_PreservesRows()
  {
    var rows = new[]
    {
      new FlatRow { Id = 1, Name = "Alice", Value = 1.5 },
      new FlatRow { Id = 2, Name = "Bob", Value = 2.5 },
    };
    var serializer = new CsvFormatSerializer<FlatRow>();
    using var stream = new MemoryStream();

    await serializer.SerializeRows(stream, AsAsync(rows));
    stream.Position = 0;
    var result = await ToList(serializer.DeserializeRows(stream));

    Assert.That(result, Has.Count.EqualTo(2));
    Assert.That(result[0], Is.EqualTo(rows[0]));
    Assert.That(result[1], Is.EqualTo(rows[1]));
  }

  [Test]
  public async Task Serialize_EmptySequence_WritesHeaderOnly()
  {
    var serializer = new CsvFormatSerializer<FlatRow>();
    using var stream = new MemoryStream();

    await serializer.SerializeRows(stream, AsAsync(Array.Empty<FlatRow>()));

    var csv = Encoding.UTF8.GetString(stream.ToArray());
    Assert.That(csv.Trim(), Is.EqualTo("Id,Name,Value"));
  }

  // ── SerializedLabel ─────────────────────────────────────────────────

  [Test]
  public async Task SerializedLabel_WritesExternalColumnNames()
  {
    var rows = new[] { new LabeledRow { CompanyId = 42, CompanyName = "Acme" } };
    var serializer = new CsvFormatSerializer<LabeledRow>();
    using var stream = new MemoryStream();

    await serializer.SerializeRows(stream, AsAsync(rows));

    var csv = Encoding.UTF8.GetString(stream.ToArray());
    Assert.That(csv, Does.Contain("company_id"));
    Assert.That(csv, Does.Contain("company_name"));
    Assert.That(csv, Does.Not.Contain("CompanyId"));
    Assert.That(csv, Does.Not.Contain("CompanyName"));
  }

  [Test]
  public async Task SerializedLabel_DeserializesFromExternalColumnNames()
  {
    const string csv = "company_id,company_name\n99,TestCo\n";
    using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
    var serializer = new CsvFormatSerializer<LabeledRow>();

    var result = await ToList(serializer.DeserializeRows(stream));

    Assert.That(result, Has.Count.EqualTo(1));
    Assert.That(result[0].CompanyId, Is.EqualTo(99));
    Assert.That(result[0].CompanyName, Is.EqualTo("TestCo"));
  }

  // ── Traits ──────────────────────────────────────────────────────────

  [Test]
  public void Traits_CanStream_IsTrue()
  {
    Assert.That(new CsvFormatSerializer<FlatRow>().Traits.CanStream, Is.True);
  }

  // ── Null-argument guards ────────────────────────────────────────────

  [Test]
  public void DeserializeRows_NullStream_ThrowsArgumentNullException()
  {
    var serializer = new CsvFormatSerializer<FlatRow>();
    Assert.ThrowsAsync<ArgumentNullException>(
      async () => await ToList(serializer.DeserializeRows(null!))
    );
  }

  [Test]
  public void SerializeRows_NullStream_ThrowsArgumentNullException()
  {
    var serializer = new CsvFormatSerializer<FlatRow>();
    Assert.ThrowsAsync<ArgumentNullException>(
      async () => await serializer.SerializeRows(null!, AsAsync(Array.Empty<FlatRow>()))
    );
  }

  // ── SerializedEnum ──────────────────────────────────────────────────

  [Test]
  public async Task SerializedEnum_RoundTripsEnumValue()
  {
    var rows = new[]
    {
      new CheckStatusRow { Id = 1, Status = CheckStatus.Complete },
      new CheckStatusRow { Id = 2, Status = CheckStatus.Incomplete },
    };
    var serializer = new CsvFormatSerializer<CheckStatusRow>();
    using var stream = new MemoryStream();

    await serializer.SerializeRows(stream, AsAsync(rows));
    var csv = Encoding.UTF8.GetString(stream.ToArray());
    Assert.That(csv, Does.Contain(",t"));
    Assert.That(csv, Does.Contain(",f"));

    stream.Position = 0;
    var result = await ToList(serializer.DeserializeRows(stream));
    Assert.That(result[0].Status, Is.EqualTo(CheckStatus.Complete));
    Assert.That(result[1].Status, Is.EqualTo(CheckStatus.Incomplete));
  }
}
