using Flowthru.Core.SourceGenerators.Schema;
using Flowthru.Data.Schema;

namespace Flowthru.Core.SourceGenerators.Tests;

/// <summary>
/// Coverage for FT2010 — the schema-property set-type analyzer.
/// Set-shaped types (HashSet, SortedSet, ISet, IReadOnlySet, immutable
/// variants) can lose System.Text.Json's converter-dispatch race and
/// round-trip as <c>{Count, Capacity, Comparer}</c> instead of a JSON
/// array — the failure mode MagicAtlas hit when a <c>.Memory()</c>
/// item was swapped to <c>.Json()</c>. Arrays, <c>List&lt;T&gt;</c>,
/// dictionaries, and the read-only family interfaces use STJ's
/// dedicated converters and remain allowed.
/// </summary>
/// <remarks>
/// Scope is deliberately narrow. The broader schema-boundary contract
/// (concrete vs. interface-typed slots, Materialized vs. Iterator vs.
/// Plan family selection) is Wave 3+ work under
/// <c>docs/scratch/flowthru-trax-roadmap.md</c>'s container-dispatch
/// matrix; its analyzers will ship alongside the matrix.
/// </remarks>
[TestFixture]
public class Ft2010SchemaCollectionShapeTests
{
  // ── Set-shaped types — flag ───────────────────────────────────────────

  [Test]
  public async Task HashSetProperty_FiresFt2010()
  {
    // The canonical MagicAtlas regression: HashSet<string> falls through
    // STJ's collection converter and round-trips as
    // {Count, Capacity, Comparer}.
    var source = """
      using System.Collections.Generic;
      using Flowthru.Data.Schema;

      namespace Sample;

      [FlowthruSchema]
      public partial record Card
      {
        public required HashSet<string> Types { get; init; }
      }
      """;

    var diags = await AnalyzerTestHarness.RunAsync(
      new SchemaCollectionShapeAnalyzer(),
      source,
      typeof(FlowthruSchemaAttribute).Assembly
    );
    var hits = diags.Where("FT2010").ToList();
    Assert.That(hits, Is.Not.Empty,
      "HashSet<T> is the canonical MagicAtlas case — FT2010 must flag it.");
    Assert.That(hits[0].GetMessage(), Does.Contain("Types"),
      "Diagnostic message must name the offending property.");
    Assert.That(hits[0].GetMessage(), Does.Contain("HashSet"),
      "Diagnostic message must name the offending type.");
  }

  [Test]
  public async Task SortedSetProperty_FiresFt2010()
  {
    var source = """
      using System.Collections.Generic;
      using Flowthru.Data.Schema;

      namespace Sample;

      [FlowthruSchema]
      public partial record Card
      {
        public required SortedSet<string> Types { get; init; }
      }
      """;

    var diags = await AnalyzerTestHarness.RunAsync(
      new SchemaCollectionShapeAnalyzer(),
      source,
      typeof(FlowthruSchemaAttribute).Assembly
    );
    Assert.That(diags.Where("FT2010").ToList(), Is.Not.Empty,
      "SortedSet<T> shares HashSet's STJ converter-dispatch hazard.");
  }

  [Test]
  public async Task ISetProperty_FiresFt2010()
  {
    // The interface declaration doesn't dodge the dispatch race — STJ
    // may still pick the object converter on the runtime concrete type.
    var source = """
      using System.Collections.Generic;
      using Flowthru.Data.Schema;

      namespace Sample;

      [FlowthruSchema]
      public partial record Card
      {
        public required ISet<string> Types { get; init; }
      }
      """;

    var diags = await AnalyzerTestHarness.RunAsync(
      new SchemaCollectionShapeAnalyzer(),
      source,
      typeof(FlowthruSchemaAttribute).Assembly
    );
    Assert.That(diags.Where("FT2010").ToList(), Is.Not.Empty,
      "ISet<T> is set-shaped at the interface level — same hazard.");
  }

  [Test]
  public async Task IReadOnlySetProperty_FiresFt2010()
  {
    var source = """
      using System.Collections.Generic;
      using Flowthru.Data.Schema;

      namespace Sample;

      [FlowthruSchema]
      public partial record Card
      {
        public required IReadOnlySet<string> Types { get; init; }
      }
      """;

    var diags = await AnalyzerTestHarness.RunAsync(
      new SchemaCollectionShapeAnalyzer(),
      source,
      typeof(FlowthruSchemaAttribute).Assembly
    );
    Assert.That(diags.Where("FT2010").ToList(), Is.Not.Empty,
      "IReadOnlySet<T> is set-shaped — read-only-ness doesn't fix the dispatch hazard.");
  }

  [Test]
  public async Task ImmutableHashSetProperty_FiresFt2010()
  {
    var source = """
      using System.Collections.Immutable;
      using Flowthru.Data.Schema;

      namespace Sample;

      [FlowthruSchema]
      public partial record Card
      {
        public required ImmutableHashSet<string> Types { get; init; }
      }
      """;

    var diags = await AnalyzerTestHarness.RunAsync(
      new SchemaCollectionShapeAnalyzer(),
      source,
      typeof(FlowthruSchemaAttribute).Assembly,
      typeof(System.Collections.Immutable.ImmutableHashSet<>).Assembly
    );
    Assert.That(diags.Where("FT2010").ToList(), Is.Not.Empty,
      "ImmutableHashSet<T> is still set-shaped at the STJ-converter layer.");
  }

  // ── Non-set collections — no diagnostic ───────────────────────────────

  [Test]
  public async Task ArrayProperty_NoFt2010()
  {
    // Arrays use STJ's dedicated array converter and round-trip reliably.
    var source = """
      using Flowthru.Data.Schema;

      namespace Sample;

      [FlowthruSchema]
      public partial record Card
      {
        public required string[] Colors { get; init; }
      }
      """;

    var diags = await AnalyzerTestHarness.RunAsync(
      new SchemaCollectionShapeAnalyzer(),
      source,
      typeof(FlowthruSchemaAttribute).Assembly
    );
    Assert.That(diags.Where("FT2010").ToList(), Is.Empty,
      "T[] is round-trip-safe under STJ — FT2010 is set-shaped only.");
  }

  [Test]
  public async Task ListProperty_NoFt2010()
  {
    var source = """
      using System.Collections.Generic;
      using Flowthru.Data.Schema;

      namespace Sample;

      [FlowthruSchema]
      public partial record Card
      {
        public required List<string> Colors { get; init; }
      }
      """;

    var diags = await AnalyzerTestHarness.RunAsync(
      new SchemaCollectionShapeAnalyzer(),
      source,
      typeof(FlowthruSchemaAttribute).Assembly
    );
    Assert.That(diags.Where("FT2010").ToList(), Is.Empty,
      "List<T> uses STJ's collection converter unambiguously.");
  }

  [Test]
  public async Task IEnumerableProperty_NoFt2010()
  {
    var source = """
      using System.Collections.Generic;
      using Flowthru.Data.Schema;

      namespace Sample;

      [FlowthruSchema]
      public partial record Card
      {
        public required IEnumerable<string> Types { get; init; }
      }
      """;

    var diags = await AnalyzerTestHarness.RunAsync(
      new SchemaCollectionShapeAnalyzer(),
      source,
      typeof(FlowthruSchemaAttribute).Assembly
    );
    Assert.That(diags.Where("FT2010").ToList(), Is.Empty);
  }

  [Test]
  public async Task IReadOnlyListProperty_NoFt2010()
  {
    var source = """
      using System.Collections.Generic;
      using Flowthru.Data.Schema;

      namespace Sample;

      [FlowthruSchema]
      public partial record Card
      {
        public required IReadOnlyList<string> Types { get; init; }
      }
      """;

    var diags = await AnalyzerTestHarness.RunAsync(
      new SchemaCollectionShapeAnalyzer(),
      source,
      typeof(FlowthruSchemaAttribute).Assembly
    );
    Assert.That(diags.Where("FT2010").ToList(), Is.Empty);
  }

  [Test]
  public async Task DictionaryProperty_NoFt2010()
  {
    // Dictionary<K,V> round-trips through STJ's dictionary converter
    // (keys become JSON object property names). Not set-shaped.
    var source = """
      using System.Collections.Generic;
      using Flowthru.Data.Schema;

      namespace Sample;

      [FlowthruSchema]
      public partial record Card
      {
        public required Dictionary<string, string> Urls { get; init; }
      }
      """;

    var diags = await AnalyzerTestHarness.RunAsync(
      new SchemaCollectionShapeAnalyzer(),
      source,
      typeof(FlowthruSchemaAttribute).Assembly
    );
    Assert.That(diags.Where("FT2010").ToList(), Is.Empty);
  }

  [Test]
  public async Task ByteArrayProperty_NoFt2010()
  {
    // byte[] is the canonical scalar binary type (FTPY2008 wire-format
    // list). Not a collection in the schema sense, and not set-shaped.
    var source = """
      using Flowthru.Data.Schema;

      namespace Sample;

      [FlowthruSchema]
      public partial record Blob
      {
        public required byte[] Payload { get; init; }
      }
      """;

    var diags = await AnalyzerTestHarness.RunAsync(
      new SchemaCollectionShapeAnalyzer(),
      source,
      typeof(FlowthruSchemaAttribute).Assembly
    );
    Assert.That(diags.Where("FT2010").ToList(), Is.Empty);
  }

  // ── Recursion — inner set under safe outer container ──────────────────

  [Test]
  public async Task IReadOnlyListOfHashSet_FlagsInnerHashSet()
  {
    // Outer IReadOnlyList is fine, but the element type is set-shaped.
    // The analyzer must recurse into generic arguments.
    var source = """
      using System.Collections.Generic;
      using Flowthru.Data.Schema;

      namespace Sample;

      [FlowthruSchema]
      public partial record Card
      {
        public required IReadOnlyList<HashSet<string>> Buckets { get; init; }
      }
      """;

    var diags = await AnalyzerTestHarness.RunAsync(
      new SchemaCollectionShapeAnalyzer(),
      source,
      typeof(FlowthruSchemaAttribute).Assembly
    );
    var hits = diags.Where("FT2010").ToList();
    Assert.That(hits, Is.Not.Empty,
      "Outer IReadOnlyList is fine, but inner HashSet must surface.");
    Assert.That(hits[0].GetMessage(), Does.Contain("HashSet"),
      "Diagnostic must name the inner offending type.");
  }

  [Test]
  public async Task ArrayOfHashSet_FlagsInnerHashSet()
  {
    var source = """
      using System.Collections.Generic;
      using Flowthru.Data.Schema;

      namespace Sample;

      [FlowthruSchema]
      public partial record Card
      {
        public required HashSet<string>[] Buckets { get; init; }
      }
      """;

    var diags = await AnalyzerTestHarness.RunAsync(
      new SchemaCollectionShapeAnalyzer(),
      source,
      typeof(FlowthruSchemaAttribute).Assembly
    );
    Assert.That(diags.Where("FT2010").ToList(), Is.Not.Empty,
      "T[] is safe but a set element under it must still surface.");
  }

  // ── Nullable wrappers — unwrap and check the underlying ───────────────

  [Test]
  public async Task NullableHashSet_FlagsUnderlying()
  {
    // value-type nullable wrapper around a set still surfaces the set.
    // (HashSet<T> is a reference type, so ? here is reference-nullability,
    // but the analyzer's unwrap path also covers the Nullable<T> case.)
    var source = """
      using System.Collections.Generic;
      using Flowthru.Data.Schema;

      namespace Sample;

      [FlowthruSchema]
      public partial record Card
      {
        public required HashSet<string>? Types { get; init; }
      }
      """;

    var diags = await AnalyzerTestHarness.RunAsync(
      new SchemaCollectionShapeAnalyzer(),
      source,
      typeof(FlowthruSchemaAttribute).Assembly
    );
    Assert.That(diags.Where("FT2010").ToList(), Is.Not.Empty,
      "Nullable wrapper must not hide the set-shaped underlying type.");
  }

  // ── Non-collection properties — no false positives ────────────────────

  [Test]
  public async Task PrimitiveProperties_NoFt2010()
  {
    var source = """
      using System;
      using Flowthru.Data.Schema;

      namespace Sample;

      [FlowthruSchema]
      public partial record Card
      {
        public required int Id { get; init; }
        public required string Name { get; init; }
        public required DateTime CreatedAt { get; init; }
        public required double? Cmc { get; init; }
      }
      """;

    var diags = await AnalyzerTestHarness.RunAsync(
      new SchemaCollectionShapeAnalyzer(),
      source,
      typeof(FlowthruSchemaAttribute).Assembly
    );
    Assert.That(diags.Where("FT2010").ToList(), Is.Empty);
  }

  [Test]
  public async Task NonSchemaTypeWithHashSet_NoFt2010()
  {
    // Only [FlowthruSchema]-decorated types are in scope.
    var source = """
      using System.Collections.Generic;

      namespace Sample;

      public record PlainPoco(HashSet<string> Tags);
      """;

    var diags = await AnalyzerTestHarness.RunAsync(
      new SchemaCollectionShapeAnalyzer(),
      source
    );
    Assert.That(diags.Where("FT2010").ToList(), Is.Empty,
      "Non-[FlowthruSchema] types are out of scope for FT2010.");
  }
}
