using System.Text;
using Flowthru.Extensions.Csv.Tests.Fixtures;
using Flowthru.Data.Storage;
using Flowthru.Data.Storage.Csv;

namespace Flowthru.Extensions.Csv.Tests;

/// <summary>
/// Null-handling for <see cref="CsvFormatSerializer{TRow}"/> — empty
/// cells, configurable null sentinels, and round-trip preservation.
/// </summary>
[TestFixture]
[Category("Csv")]
public class CsvNullHandlingTests
{
  private static async IAsyncEnumerable<T> AsAsync<T>(IEnumerable<T> source)
  {
    foreach (var item in source)
    {
      yield return item;
      await Task.Yield();
    }
  }

  private static async Task<List<T>> Deserialize<T>(IFormatSerializer<T> serializer, string csv)
    where T : notnull
  {
    using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
    var rows = new List<T>();
    await foreach (var row in serializer.DeserializeRows(stream))
    {
      rows.Add(row);
    }
    return rows;
  }

  [Test]
  public async Task DefaultBehavior_EmptyCellsBecomeNull_ForNullableProperties()
  {
    var serializer = new CsvFormatSerializer<NullableRow>();
    var csv = """
      id,nullable_name,non_nullable_name,nullable_value
      1,Alice,Aldous,42
      2,,Bob,
      """;

    var rows = await Deserialize(serializer, csv);

    Assert.That(rows, Has.Count.EqualTo(2));
    Assert.That(rows[0].NullableName, Is.EqualTo("Alice"));
    Assert.That(rows[0].NullableValue, Is.EqualTo(42));
    Assert.That(rows[1].NullableName, Is.Null,
      "Empty cell should deserialize to null for string?");
    Assert.That(rows[1].NullableValue, Is.Null,
      "Empty cell should deserialize to null for int?");
  }

  [Test]
  public async Task DefaultBehavior_EmptyCellsLeaveNonNullableStringAsEmpty()
  {
    var serializer = new CsvFormatSerializer<NullableRow>();
    var csv = """
      id,nullable_name,non_nullable_name,nullable_value
      1,Alice,,42
      """;

    var rows = await Deserialize(serializer, csv);

    Assert.That(rows[0].NonNullableName, Is.EqualTo(string.Empty),
      "Non-nullable string field should preserve empty-string semantics, not become null.");
  }

  [Test]
  public async Task CustomNullValues_PandasStyleSentinels_DeserializeToNull()
  {
    var serializer = new CsvFormatSerializer<NullableRow>(
      nullValues: new[] { string.Empty, "NA", "N/A", "NULL" }
    );
    var csv = """
      id,nullable_name,non_nullable_name,nullable_value
      1,NA,Alice,
      2,N/A,Bob,NULL
      3,Charlie,Charles,7
      """;

    var rows = await Deserialize(serializer, csv);

    Assert.That(rows, Has.Count.EqualTo(3));
    Assert.That(rows[0].NullableName, Is.Null, "'NA' should be treated as null");
    Assert.That(rows[1].NullableName, Is.Null, "'N/A' should be treated as null");
    Assert.That(rows[1].NullableValue, Is.Null, "'NULL' should be treated as null for int?");
    Assert.That(rows[2].NullableName, Is.EqualTo("Charlie"));
    Assert.That(rows[2].NullableValue, Is.EqualTo(7));
  }

  [Test]
  public async Task CustomNullValues_NonNullableStringStillEmptyOnEmptyCell()
  {
    var serializer = new CsvFormatSerializer<NullableRow>(
      nullValues: new[] { string.Empty, "NA" }
    );
    var csv = """
      id,nullable_name,non_nullable_name,nullable_value
      1,Alice,,42
      """;

    var rows = await Deserialize(serializer, csv);

    Assert.That(rows[0].NonNullableName, Is.EqualTo(string.Empty),
      "Custom null-values should not affect non-nullable string properties.");
  }

  [Test]
  public async Task RoundTrip_NullableValues_PreservedAcrossSerialize()
  {
    var serializer = new CsvFormatSerializer<NullableRow>();
    var input = new[]
    {
      new NullableRow
      {
        Id = 1,
        NullableName = "Alice",
        NonNullableName = "Aldous",
        NullableValue = 42,
      },
      new NullableRow
      {
        Id = 2,
        NullableName = null,
        NonNullableName = "Bob",
        NullableValue = null,
      },
    };

    using var buffer = new MemoryStream();
    await serializer.SerializeRows(buffer, AsAsync(input));
    buffer.Position = 0;

    var output = new List<NullableRow>();
    await foreach (var row in serializer.DeserializeRows(buffer))
    {
      output.Add(row);
    }

    Assert.That(output, Has.Count.EqualTo(2));
    Assert.That(output[0], Is.EqualTo(input[0]));
    Assert.That(output[1], Is.EqualTo(input[1]));
  }
}
