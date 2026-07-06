using Flowthru.Core.SourceGenerators.Storage;

namespace Flowthru.Core.SourceGenerators.Tests;

/// <summary>
/// Tests for <see cref="StreamingTraitHonestyAnalyzer"/> — the <c>FT1201</c>
/// analyzer that enforces "declared <c>StorageTraits.CanStream = true</c> ⇒ the
/// format actually streams". Covers both sub-cases (the missing
/// <c>IFormatStreamReader&lt;TRow&gt;</c> marker, and a <c>DeserializeRows</c>
/// body that materialises the whole input) and the low-false-positive guards
/// (honest streaming readers, bounded metadata <c>ToList</c>, non-format types).
/// </summary>
[TestFixture]
public class Ft1201StreamingTraitHonestyTests
{
  // ── Stubs ─────────────────────────────────────────────────────────────
  //
  // Minimal stand-ins for the storage surface. The analyzer gates on the
  // IFormatRowReader<TRow>/IFormatStreamReader<TRow> interfaces by
  // fully-qualified name, reads CanStream from an object-initializer literal,
  // and scans DeserializeRows syntactically — so the stubs need no real
  // System.Text.Json / async-stream machinery.

  private const string Stubs = """
    namespace Flowthru.Data.Storage
    {
      public record StorageTraits
      {
        public bool CanStream { get; init; }
      }

      public interface IFormatRowReader<TRow>
      {
        StorageTraits Traits { get; }
      }

      public interface IFormatStreamReader<TRow> : IFormatRowReader<TRow> { }
    }

    namespace Sample
    {
      // Trailing-name match is all the analyzer needs for the JSON branch, so a
      // local stub named JsonSerializer stands in for System.Text.Json's.
      public static class JsonSerializer
      {
        public static System.Collections.Generic.IEnumerable<T> Deserialize<T>(System.IO.Stream s) =>
          System.Linq.Enumerable.Empty<T>();
        public static System.Collections.Generic.IEnumerable<T> DeserializeAsyncEnumerable<T>(System.IO.Stream s) =>
          System.Linq.Enumerable.Empty<T>();
      }

      public record Row(int Id);
    }
    """;

  // ── Honest streaming reader is silent ────────────────────────────────

  [Test]
  public async Task StreamingReader_YieldingIncrementally_Silent()
  {
    var consumer = """
      using System.Collections.Generic;
      using System.IO;
      using Flowthru.Data.Storage;

      namespace Sample;

      public sealed class HonestJsonSerializer : IFormatStreamReader<Row>
      {
        public StorageTraits Traits => new() { CanStream = true };

        public IEnumerable<Row> DeserializeRows(Stream stream)
        {
          foreach (var r in JsonSerializer.DeserializeAsyncEnumerable<Row>(stream))
          {
            yield return r;
          }
        }
      }
      """;

    var diags = await AnalyzerTestHarness.RunAsync(
      new StreamingTraitHonestyAnalyzer(),
      new[] { Stubs, consumer }
    );
    Assert.That(diags.Where("FT1201").ToList(), Is.Empty,
      "An honest streaming reader (marker + incremental yield) must not fire FT1201.");
  }

  // ── Marker honesty branch ────────────────────────────────────────────

  [Test]
  public async Task CanStreamTrue_WithoutStreamReaderMarker_FiresFt1201()
  {
    var consumer = """
      using System.Collections.Generic;
      using System.IO;
      using Flowthru.Data.Storage;

      namespace Sample;

      public sealed class MarkerlessSerializer : IFormatRowReader<Row>
      {
        public StorageTraits Traits => new() { CanStream = true };

        public IEnumerable<Row> DeserializeRows(Stream stream)
        {
          foreach (var r in JsonSerializer.DeserializeAsyncEnumerable<Row>(stream))
          {
            yield return r;
          }
        }
      }
      """;

    var diags = await AnalyzerTestHarness.RunAsync(
      new StreamingTraitHonestyAnalyzer(),
      new[] { Stubs, consumer }
    );

    var ft1201 = diags.Where("FT1201").ToList();
    Assert.That(ft1201, Is.Not.Empty);
    Assert.That(ft1201[0].GetMessage(), Does.Contain("IFormatStreamReader"));
  }

  // ── Body honesty branch: whole-document JSON ─────────────────────────

  [Test]
  public async Task DeserializeRows_UsesWholeDocumentJsonDeserialize_FiresFt1201()
  {
    var consumer = """
      using System.Collections.Generic;
      using System.IO;
      using Flowthru.Data.Storage;

      namespace Sample;

      public sealed class BufferingJsonSerializer : IFormatStreamReader<Row>
      {
        public StorageTraits Traits => new() { CanStream = true };

        public IEnumerable<Row> DeserializeRows(Stream stream)
        {
          // Dishonest: buffers the entire document, then yields.
          var all = JsonSerializer.Deserialize<Row>(stream);
          foreach (var r in all)
          {
            yield return r;
          }
        }
      }
      """;

    var diags = await AnalyzerTestHarness.RunAsync(
      new StreamingTraitHonestyAnalyzer(),
      new[] { Stubs, consumer }
    );

    var ft1201 = diags.Where("FT1201").ToList();
    Assert.That(ft1201, Is.Not.Empty);
    Assert.That(ft1201[0].GetMessage(), Does.Contain("JsonSerializer.Deserialize"));
  }

  // ── Body honesty branch: ToList over the input stream ────────────────

  [Test]
  public async Task DeserializeRows_MaterialisesInputStreamWithToList_FiresFt1201()
  {
    var consumer = """
      using System.Collections.Generic;
      using System.IO;
      using System.Linq;
      using Flowthru.Data.Storage;

      namespace Sample;

      public sealed class ToListSerializer : IFormatStreamReader<Row>
      {
        public StorageTraits Traits => new() { CanStream = true };

        public IEnumerable<Row> DeserializeRows(Stream stream)
        {
          // Dishonest: drains the whole stream-derived enumerable into a List.
          var all = JsonSerializer.DeserializeAsyncEnumerable<Row>(stream).ToList();
          foreach (var r in all)
          {
            yield return r;
          }
        }
      }
      """;

    var diags = await AnalyzerTestHarness.RunAsync(
      new StreamingTraitHonestyAnalyzer(),
      new[] { Stubs, consumer }
    );

    var ft1201 = diags.Where("FT1201").ToList();
    Assert.That(ft1201, Is.Not.Empty);
    Assert.That(ft1201[0].GetMessage(), Does.Contain("ToList"));
  }

  // ── Low-false-positive guard: bounded ToList over metadata (Parquet shape) ──

  [Test]
  public async Task DeserializeRows_ToListOverMetadataNotStream_Silent()
  {
    // Mirrors ParquetFormatSerializer: a `.ToList()` over a small column-name
    // set (never the input stream). Must NOT fire — a false positive here would
    // break the Parquet build.
    var consumer = """
      using System.Collections.Generic;
      using System.IO;
      using System.Linq;
      using Flowthru.Data.Storage;

      namespace Sample;

      public sealed class RowGroupSerializer : IFormatStreamReader<Row>
      {
        public StorageTraits Traits => new() { CanStream = true };

        public IEnumerable<Row> DeserializeRows(Stream stream)
        {
          var expected = new[] { "a", "b", "c" };
          var present = new HashSet<string> { "a", "b" };
          var missing = expected.Where(c => !present.Contains(c)).ToList();
          foreach (var r in JsonSerializer.DeserializeAsyncEnumerable<Row>(stream))
          {
            yield return r;
          }
        }
      }
      """;

    var diags = await AnalyzerTestHarness.RunAsync(
      new StreamingTraitHonestyAnalyzer(),
      new[] { Stubs, consumer }
    );
    Assert.That(diags.Where("FT1201").ToList(), Is.Empty,
      "A bounded ToList over metadata (not the input stream) must not fire FT1201.");
  }

  // ── CanStream not declared: body is not policed ──────────────────────

  [Test]
  public async Task BufferingBody_WithoutCanStream_Silent()
  {
    var consumer = """
      using System.Collections.Generic;
      using System.IO;
      using Flowthru.Data.Storage;

      namespace Sample;

      public sealed class HonestlyBufferedSerializer : IFormatRowReader<Row>
      {
        // CanStream defaults to false — buffering is legitimate.
        public StorageTraits Traits => new();

        public IEnumerable<Row> DeserializeRows(Stream stream)
        {
          var all = JsonSerializer.Deserialize<Row>(stream);
          foreach (var r in all)
          {
            yield return r;
          }
        }
      }
      """;

    var diags = await AnalyzerTestHarness.RunAsync(
      new StreamingTraitHonestyAnalyzer(),
      new[] { Stubs, consumer }
    );
    Assert.That(diags.Where("FT1201").ToList(), Is.Empty,
      "A buffering reader that honestly leaves CanStream = false must not fire FT1201.");
  }

  // ── Non-format types are out of scope ────────────────────────────────

  [Test]
  public async Task NonFormatTypeWithCanStream_Silent()
  {
    // Represents a storage adapter/medium (EFCore, S3) that carries CanStream on
    // its own traits but is not an IFormatRowReader — out of the analyzer's scope.
    var consumer = """
      using Flowthru.Data.Storage;

      namespace Sample;

      public sealed class BulkAdapter
      {
        public StorageTraits Traits => new() { CanStream = true };
      }
      """;

    var diags = await AnalyzerTestHarness.RunAsync(
      new StreamingTraitHonestyAnalyzer(),
      new[] { Stubs, consumer }
    );
    Assert.That(diags.Where("FT1201").ToList(), Is.Empty,
      "A non-IFormatRowReader type must not fire FT1201 even with CanStream = true.");
  }

  [Test]
  public void SupportedDiagnostics_ExposesFt1201()
  {
    var analyzer = new StreamingTraitHonestyAnalyzer();
    Assert.That(analyzer.SupportedDiagnostics.Select(d => d.Id),
      Has.Member("FT1201"));
  }
}
