using Flowthru.Data.Storage;
using Flowthru.Data.Storage.Parquet;
using Flowthru.Extensions.Parquet.Tests.Fixtures;

namespace Flowthru.Extensions.Parquet.Tests;

/// <summary>
/// Direct exercises of <see cref="ParquetFormatSerializer{TRow}"/> on
/// flat schemas — round-trip, schema-mismatch detection, and trait /
/// marker claims.
/// </summary>
[TestFixture]
[Category("Parquet")]
public class ParquetFormatSerializerTests
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
    await foreach (var item in source) list.Add(item);
    return list;
  }

  // ── Traits ──────────────────────────────────────────────────────────

  [Test]
  public void Traits_CanStream_IsTrue()
  {
    Assert.That(new ParquetFormatSerializer<FlatRow>().Traits.CanStream, Is.True,
      "Parquet's row-group cursor enables genuine streaming for early-break consumers.");
  }

  [Test]
  public void Type_ImplementsStreamReaderMarker()
  {
    Assert.That(new ParquetFormatSerializer<FlatRow>(),
      Is.AssignableTo<IFormatStreamReader<FlatRow>>(),
      "Parquet streams row groups; structural marker should match the runtime trait."
    );
  }

  // ── Round-trip ──────────────────────────────────────────────────────

  [Test]
  public async Task RoundTrip_FlatRow_PreservesAllRows()
  {
    var rows = new[]
    {
      new FlatRow { Id = 1, Name = "Alice", Value = 1.5 },
      new FlatRow { Id = 2, Name = "Bob",   Value = 2.5 },
      new FlatRow { Id = 3, Name = "Carol", Value = 3.5 },
    };

    var serializer = new ParquetFormatSerializer<FlatRow>();
    using var stream = new MemoryStream();
    await serializer.SerializeRows(stream, AsAsync(rows));
    stream.Position = 0;

    var loaded = await ToList(serializer.DeserializeRows(stream));
    Assert.That(loaded, Has.Count.EqualTo(3));
    Assert.That(loaded[0], Is.EqualTo(rows[0]));
    Assert.That(loaded[1], Is.EqualTo(rows[1]));
    Assert.That(loaded[2], Is.EqualTo(rows[2]));
  }

  [Test]
  public async Task RoundTrip_LabeledRow_HonorsSerializedLabel()
  {
    var rows = new[]
    {
      new LabeledRow { CompanyId = 42, CompanyName = "Acme" },
      new LabeledRow { CompanyId = 99, CompanyName = "TestCo" },
    };

    var serializer = new ParquetFormatSerializer<LabeledRow>();
    using var stream = new MemoryStream();
    await serializer.SerializeRows(stream, AsAsync(rows));
    stream.Position = 0;

    var loaded = await ToList(serializer.DeserializeRows(stream));
    Assert.That(loaded[0], Is.EqualTo(rows[0]));
    Assert.That(loaded[1], Is.EqualTo(rows[1]));
  }

  [Test]
  public async Task RoundTrip_NullableRow_PreservesNulls()
  {
    var rows = new[]
    {
      new NullableRow { Id = 1, OptionalName = "Alice", OptionalCount = 7 },
      new NullableRow { Id = 2, OptionalName = null,    OptionalCount = null },
    };

    var serializer = new ParquetFormatSerializer<NullableRow>();
    using var stream = new MemoryStream();
    await serializer.SerializeRows(stream, AsAsync(rows));
    stream.Position = 0;

    var loaded = await ToList(serializer.DeserializeRows(stream));
    Assert.That(loaded, Has.Count.EqualTo(2));
    Assert.That(loaded[0].OptionalName, Is.EqualTo("Alice"));
    Assert.That(loaded[0].OptionalCount, Is.EqualTo(7));
    Assert.That(loaded[1].OptionalName, Is.Null);
    Assert.That(loaded[1].OptionalCount, Is.Null);
  }

  [Test]
  public async Task RoundTrip_EmptyEnumerable_ProducesValidParquetWithZeroRows()
  {
    var serializer = new ParquetFormatSerializer<FlatRow>();
    using var stream = new MemoryStream();
    await serializer.SerializeRows(stream, AsAsync(Array.Empty<FlatRow>()));
    Assert.That(stream.Length, Is.GreaterThan(0),
      "Even empty Parquet writes a footer/schema, not zero bytes.");

    stream.Position = 0;
    var loaded = await ToList(serializer.DeserializeRows(stream));
    Assert.That(loaded, Is.Empty);
  }

  // ── Schema mismatch ─────────────────────────────────────────────────

  [Test]
  public void DeserializeRows_FileMissingDeclaredColumn_ThrowsSchemaMismatch()
  {
    // Write a file under one schema, then try to read it under a wider schema
    // that requires an additional column. Parquet.Net would silently default-fill
    // the missing column without our pre-flight schema check; we standardize on
    // throwing SchemaMismatchException so the composed adapter lifts to typed
    // RuntimeError.SchemaMismatch / ValidationErrorType.SchemaMismatch.
    var slim = new ParquetFormatSerializer<MismatchSlim>();
    var fat = new ParquetFormatSerializer<MismatchFat>();

    using var stream = new MemoryStream();
    slim.SerializeRows(stream, AsAsync(new[]
    {
      new MismatchSlim { Id = 1, Name = "Alice" },
    })).GetAwaiter().GetResult();
    stream.Position = 0;

    Assert.ThrowsAsync<SchemaMismatchException>(
      async () => await ToList(fat.DeserializeRows(stream)),
      "A column declared by TRow but absent in the file must surface as "
        + "SchemaMismatchException — the cross-format contract that lets "
        + "ComposedStorageAdapter classify it as ValidationErrorType.SchemaMismatch."
    );
  }
}

/// <summary>Narrow schema for the schema-mismatch test fixture.</summary>
[Flowthru.Data.Schema.FlowthruSchema]
public partial record MismatchSlim
{
  public required int Id { get; init; }
  public required string Name { get; init; }
}

/// <summary>Wide schema for the schema-mismatch test fixture — adds a column the slim file lacks.</summary>
[Flowthru.Data.Schema.FlowthruSchema]
public partial record MismatchFat
{
  public required int Id { get; init; }
  public required string Name { get; init; }
  public required double Score { get; init; }
}
