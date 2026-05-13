using Flowthru.Core.SourceGenerators.Schema;
using Flowthru.Data.Schema;

namespace Flowthru.Core.SourceGenerators.Tests;

/// <summary>
/// Behavioural tests for <see cref="SchemaInterfaceGenerator"/>. The
/// generator inspects <c>[FlowthruSchema]</c> types and emits a partial
/// declaration carrying the appropriate marker interfaces — flat schemas
/// get <c>IFlatSchema</c> plus the text/binary/structured serializable
/// triple, nested schemas get <c>INestedSchema</c> plus
/// <c>IStructuredSerializable</c>. Non-partial types are silently
/// skipped (the analyzer fires FT1001 separately), and manually-applied
/// interfaces are stripped from the generated interface list to avoid
/// duplicate-declaration errors.
/// </summary>
[TestFixture]
public class SchemaInterfaceGeneratorTests
{
  // ── Partial gating ────────────────────────────────────────────────────

  [Test]
  public void NonPartialSchema_EmitsNothing()
  {
    // The analyzer raises FT1001 for non-partial types; the generator
    // itself skips emission so the user doesn't get cascading errors
    // on a generated partial that has nothing to attach to.
    var source = """
      using Flowthru.Data.Schema;
      namespace Sample;

      [FlowthruSchema]
      public record NotPartial(int X);
      """;

    var result = AnalyzerTestHarness.RunGenerator(
      new SchemaInterfaceGenerator(),
      source,
      typeof(FlowthruSchemaAttribute).Assembly
    );

    Assert.That(result.GeneratedSources, Is.Empty,
      "Non-partial schema types must not produce generated source.");
  }

  // ── Flat schema emission ──────────────────────────────────────────────

  [Test]
  public void PrimitivePropertiesOnly_EmitsFlatSchemaWithSerializableMarkers()
  {
    var source = """
      using Flowthru.Data.Schema;
      namespace Sample;

      [FlowthruSchema]
      public partial record Pure
      {
        public required int A { get; init; }
        public required string B { get; init; }
        public required bool C { get; init; }
      }
      """;

    var emitted = EmitFirstSource(source);

    Assert.That(emitted, Does.Contain("partial record Pure :"));
    Assert.That(emitted, Does.Contain("global::Flowthru.Data.Schema.IFlatSchema"));
    Assert.That(emitted, Does.Contain("global::Flowthru.Data.Schema.ITextSerializable"));
    Assert.That(emitted, Does.Contain("global::Flowthru.Data.Schema.IBinarySerializable"));
    Assert.That(emitted, Does.Contain("global::Flowthru.Data.Schema.IStructuredSerializable"));
    Assert.That(emitted, Does.Not.Contain("global::Flowthru.Data.Schema.INestedSchema"),
      "A flat schema must not pick up INestedSchema in addition to IFlatSchema.");
  }

  [Test]
  public void EmptySchema_IsVacuouslyFlat()
  {
    // SchemaPropertyClassifier.Classify treats zero-property schemas
    // as flat — the generator should follow.
    var source = """
      using Flowthru.Data.Schema;
      namespace Sample;

      [FlowthruSchema]
      public partial record Empty { }
      """;

    var emitted = EmitFirstSource(source);
    Assert.That(emitted, Does.Contain("global::Flowthru.Data.Schema.IFlatSchema"),
      "Empty schemas should be classified as flat.");
  }

  [Test]
  public void ByteArrayProperty_IsFlat()
  {
    // Tier 3 of the cascade: byte[] is treated as an opaque blob, not
    // as a traversable collection.
    var source = """
      using Flowthru.Data.Schema;
      namespace Sample;

      [FlowthruSchema]
      public partial record Blob
      {
        public required byte[] Bytes { get; init; }
      }
      """;

    var emitted = EmitFirstSource(source);
    Assert.That(emitted, Does.Contain("global::Flowthru.Data.Schema.IFlatSchema"),
      "byte[] should classify as a flat property (Tier 3 opaque blob).");
  }

  [Test]
  public void EnumProperty_IsFlat()
  {
    var source = """
      using Flowthru.Data.Schema;
      namespace Sample;

      public enum Color { Red, Green, Blue }

      [FlowthruSchema]
      public partial record Painted
      {
        public required Color Hue { get; init; }
      }
      """;

    var emitted = EmitFirstSource(source);
    Assert.That(emitted, Does.Contain("global::Flowthru.Data.Schema.IFlatSchema"),
      "Enums are scalar regardless of underlying integer type.");
  }

  [Test]
  public void GuidProperty_IsFlat()
  {
    // Tier 4: System.Guid is a recognized BCL scalar struct.
    var source = """
      using Flowthru.Data.Schema;
      namespace Sample;

      [FlowthruSchema]
      public partial record WithGuid
      {
        public required System.Guid Id { get; init; }
      }
      """;

    var emitted = EmitFirstSource(source);
    Assert.That(emitted, Does.Contain("global::Flowthru.Data.Schema.IFlatSchema"));
  }

  [Test]
  public void NullablePrimitiveProperty_IsFlat()
  {
    // Nullable<T> unwraps before classification — int? should still
    // produce a flat schema.
    var source = """
      using Flowthru.Data.Schema;
      namespace Sample;

      [FlowthruSchema]
      public partial record Maybe
      {
        public int? X { get; init; }
      }
      """;

    var emitted = EmitFirstSource(source);
    Assert.That(emitted, Does.Contain("global::Flowthru.Data.Schema.IFlatSchema"));
  }

  // ── Nested schema emission ────────────────────────────────────────────

  [Test]
  public void NestedReferenceProperty_EmitsNestedSchemaWithStructuredSerializable()
  {
    // A property whose type is a plain user record (no IScalar, no
    // [FlowthruColumn]) is treated as nested.
    var source = """
      using Flowthru.Data.Schema;
      namespace Sample;

      public record Inner(int A);

      [FlowthruSchema]
      public partial record Outer
      {
        public required Inner Nested { get; init; }
      }
      """;

    var emitted = EmitFirstSource(source);
    Assert.That(emitted, Does.Contain("global::Flowthru.Data.Schema.INestedSchema"),
      "A schema with non-scalar properties should be classified as nested.");
    Assert.That(emitted, Does.Contain("global::Flowthru.Data.Schema.IStructuredSerializable"),
      "Nested schemas must still carry IStructuredSerializable.");
    Assert.That(emitted, Does.Not.Contain("global::Flowthru.Data.Schema.IFlatSchema"),
      "A nested schema must not pick up IFlatSchema.");
    Assert.That(emitted, Does.Not.Contain("global::Flowthru.Data.Schema.ITextSerializable"),
      "Nested schemas should not advertise text serialization.");
    Assert.That(emitted, Does.Not.Contain("global::Flowthru.Data.Schema.IBinarySerializable"),
      "Nested schemas should not advertise binary serialization.");
  }

  // ── Manual marker stripping ───────────────────────────────────────────

  [Test]
  public void ManualIFlatSchema_OmittedFromGeneratedInterfaceList()
  {
    // Re-emitting IFlatSchema when the user already wrote it would
    // produce CS8646. Strip the manually-applied marker — analyzer
    // FT1002 still warns about it separately.
    var source = """
      using Flowthru.Data.Schema;
      namespace Sample;

      [FlowthruSchema]
      public partial record HasManualMarker : IFlatSchema
      {
        public required int X { get; init; }
      }
      """;

    var emitted = EmitFirstSource(source);
    // Strip whitespace so we only assert on the post-`:` interface list
    // (not the user's own `: IFlatSchema` declaration).
    var generatedInterfaceList = emitted.Split(new[] { "partial record HasManualMarker :" }, System.StringSplitOptions.None)[1];
    Assert.That(generatedInterfaceList, Does.Not.Contain("IFlatSchema"),
      "Manually-applied IFlatSchema must be omitted from the generated interface list.");
    // The other markers should still appear.
    Assert.That(generatedInterfaceList, Does.Contain("ITextSerializable"));
    Assert.That(generatedInterfaceList, Does.Contain("IBinarySerializable"));
    Assert.That(generatedInterfaceList, Does.Contain("IStructuredSerializable"));
  }

  [Test]
  public void AllFourFlatMarkersManual_NoEmission()
  {
    // If every interface in the generated list is already applied
    // manually, the interface list collapses to zero — the generator
    // short-circuits before AddSource to avoid emitting an empty
    // `partial record T : { }`.
    var source = """
      using Flowthru.Data.Schema;
      namespace Sample;

      [FlowthruSchema]
      public partial record AllManual
        : IFlatSchema, ITextSerializable, IBinarySerializable, IStructuredSerializable
      {
        public required int X { get; init; }
      }
      """;

    var result = AnalyzerTestHarness.RunGenerator(
      new SchemaInterfaceGenerator(),
      source,
      typeof(FlowthruSchemaAttribute).Assembly
    );

    Assert.That(result.GeneratedSources, Is.Empty,
      "When the user already applied every relevant interface manually, the generator should emit nothing.");
  }

  // ── FlowthruColumn promotion ──────────────────────────────────────────

  [Test]
  public void PropertyWithFlowthruColumnAttribute_IsTreatedAsFlat()
  {
    // A property annotated with [FlowthruColumn] is treated as flat
    // even if the declared type would normally be classified as
    // nested — the column attribute promises a generated NewType
    // will be emitted to back the property.
    var source = """
      using Flowthru.Data.Schema;
      namespace Sample;

      [FlowthruSchema]
      public partial record WithColumn
      {
        [FlowthruColumn(typeof(int))]
        public ShuttleId ShuttleId { get; init; } = default!;
      }
      """;

    var emitted = EmitFirstSource(source);
    Assert.That(emitted, Does.Contain("global::Flowthru.Data.Schema.IFlatSchema"),
      "A property carrying [FlowthruColumn] should classify the schema as flat regardless of declared type.");
  }

  // ── Hint-name / output shape ──────────────────────────────────────────

  [Test]
  public void EmittedSource_HasAutoGeneratedHeaderAndExpectedHintName()
  {
    var source = """
      using Flowthru.Data.Schema;
      namespace Sample;

      [FlowthruSchema]
      public partial record Sentinel
      {
        public required int X { get; init; }
      }
      """;

    var result = AnalyzerTestHarness.RunGenerator(
      new SchemaInterfaceGenerator(),
      source,
      typeof(FlowthruSchemaAttribute).Assembly
    );

    var match = result.GeneratedSources
      .FirstOrDefault(g => g.HintName == "Sentinel.SchemaInterfaces.g.cs");
    Assert.That(match.Source, Is.Not.Null,
      "Hint name should be `{TypeName}.SchemaInterfaces.g.cs`. Generated: "
      + string.Join(", ", result.GeneratedSources.Select(g => g.HintName)));
    Assert.That(match.Source, Does.Contain("// <auto-generated/>"));
    Assert.That(match.Source, Does.Contain("#nullable enable"));
    Assert.That(match.Source, Does.Contain("namespace Sample;"));
  }

  // ── helper ────────────────────────────────────────────────────────────

  private static string EmitFirstSource(string source)
  {
    var result = AnalyzerTestHarness.RunGenerator(
      new SchemaInterfaceGenerator(),
      source,
      typeof(FlowthruSchemaAttribute).Assembly
    );
    Assert.That(result.GeneratedSources, Is.Not.Empty,
      "Expected at least one generated file for the given schema source.");
    return result.GeneratedSources[0].Source;
  }
}
