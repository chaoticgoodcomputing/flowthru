using Flowthru.Core.Data.Capabilities;
using Flowthru.Core.Data.Storage;
using Flowthru.Core.Data.Storage.Format;
using Flowthru.Tests.Kits.Format;
using Flowthru.Tests.Kits.Schemas;

namespace Flowthru.Core.Tests.Conformance;

/// <summary>
/// Conformance subclasses for <see cref="JsonFormatSerializer{TRow}"/>. Unlike CSV /
/// Excel / Parquet (extensions), JSON ships in Core, so its conformance suite lives in
/// <c>Flowthru.Core.Tests</c> alongside the JSON storage-adapter conformance subclasses.
///
/// JSON consumes <see cref="Flowthru.Core.Data.Serialization.PropertyMappingPlanner"/>
/// (Phase B4) and supports both flat and nested row shapes. The conformance subclasses
/// below cover every kit fixture JSON's <see cref="IFormatSerializer{TRow}.RowFeatures"/>
/// claim admits.
/// </summary>

// ── Flat fixtures ───────────────────────────────────────────────────────────

[TestFixtureSource(nameof(Fixtures))]
public class JsonTraditionalSchemaConformance : FormatSerializerConformance<TraditionalSchema>
{
  public static IEnumerable<string> Fixtures => new[] { "Flat/Simple/rows.json" };

  public JsonTraditionalSchemaConformance(string fixturePath) : base(fixturePath) { }

  protected override IFormatSerializer<TraditionalSchema> CreateSerializer() =>
    new JsonFormatSerializer<TraditionalSchema>();
}

[TestFixtureSource(nameof(Fixtures))]
public class JsonRequiredMembersConformance : FormatSerializerConformance<RequiredMembersSchema>
{
  public static IEnumerable<string> Fixtures => new[] { "Flat/RequiredMembers/rows.json" };

  public JsonRequiredMembersConformance(string fixturePath) : base(fixturePath) { }

  protected override IFormatSerializer<RequiredMembersSchema> CreateSerializer() =>
    new JsonFormatSerializer<RequiredMembersSchema>();
}

[TestFixtureSource(nameof(Fixtures))]
public class JsonMixedRequirementsConformance : FormatSerializerConformance<MixedRequirementsSchema>
{
  public static IEnumerable<string> Fixtures => new[] { "Flat/MixedRequirements/rows.json" };

  public JsonMixedRequirementsConformance(string fixturePath) : base(fixturePath) { }

  protected override IFormatSerializer<MixedRequirementsSchema> CreateSerializer() =>
    new JsonFormatSerializer<MixedRequirementsSchema>();
}

[TestFixtureSource(nameof(Fixtures))]
public class JsonPositionalRecordConformance : FormatSerializerConformance<PositionalRecordSchema>
{
  public static IEnumerable<string> Fixtures => new[] { "Flat/PositionalRecord/rows.json" };

  public JsonPositionalRecordConformance(string fixturePath) : base(fixturePath) { }

  protected override IFormatSerializer<PositionalRecordSchema> CreateSerializer() =>
    new JsonFormatSerializer<PositionalRecordSchema>();
}

[TestFixtureSource(nameof(Fixtures))]
public class JsonCheckStatusConformance : FormatSerializerConformance<CheckStatusSchema>
{
  public static IEnumerable<string> Fixtures => new[] { "Flat/SerializedEnum/rows.json" };

  public JsonCheckStatusConformance(string fixturePath) : base(fixturePath) { }

  protected override IFormatSerializer<CheckStatusSchema> CreateSerializer() =>
    new JsonFormatSerializer<CheckStatusSchema>();
}

[TestFixtureSource(nameof(Fixtures))]
public class JsonMultiEnumConformance : FormatSerializerConformance<MultiEnumSchema>
{
  public static IEnumerable<string> Fixtures => new[] { "Flat/MultiEnum/rows.json" };

  public JsonMultiEnumConformance(string fixturePath) : base(fixturePath) { }

  protected override IFormatSerializer<MultiEnumSchema> CreateSerializer() =>
    new JsonFormatSerializer<MultiEnumSchema>();
}

[TestFixtureSource(nameof(Fixtures))]
public class JsonOptionalEnumConformance : FormatSerializerConformance<OptionalEnumSchema>
{
  public static IEnumerable<string> Fixtures => new[] { "Flat/OptionalEnum/rows.json" };

  public JsonOptionalEnumConformance(string fixturePath) : base(fixturePath) { }

  protected override IFormatSerializer<OptionalEnumSchema> CreateSerializer() =>
    new JsonFormatSerializer<OptionalEnumSchema>();
}

[TestFixtureSource(nameof(Fixtures))]
public class JsonIScalarConformance : FormatSerializerConformance<IScalarSchema>
{
  public static IEnumerable<string> Fixtures => new[] { "Flat/IScalar/rows.json" };

  public JsonIScalarConformance(string fixturePath) : base(fixturePath) { }

  protected override IFormatSerializer<IScalarSchema> CreateSerializer() =>
    new JsonFormatSerializer<IScalarSchema>();

  protected override Func<FormatRowFeatures, bool>? RequiredFeatures =>
    f => f.SupportsIScalar;
}

[TestFixtureSource(nameof(Fixtures))]
public class JsonMultiIScalarConformance : FormatSerializerConformance<MultiIScalarSchema>
{
  public static IEnumerable<string> Fixtures => new[] { "Flat/MultiIScalar/rows.json" };

  public JsonMultiIScalarConformance(string fixturePath) : base(fixturePath) { }

  protected override IFormatSerializer<MultiIScalarSchema> CreateSerializer() =>
    new JsonFormatSerializer<MultiIScalarSchema>();

  protected override Func<FormatRowFeatures, bool>? RequiredFeatures =>
    f => f.SupportsIScalar;
}

/// <remarks>
/// JSON-specific regression test for <c>byte[]</c> round-trip via System.Text.Json's
/// base64 handling. <c>byte[]</c> is not a tracked row-shape capability in the matrix —
/// it's a primitive-level format-mechanics concern (CsvHelper, ExcelDataReader, Parquet,
/// and JsonSerializer each handle byte arrays through their respective primitive
/// converters). This subclass exists as JSON regression coverage; if a parallel test for
/// CSV/Excel/Parquet is wanted later, add the subclass without a <c>RequiredFeatures</c>
/// override.
/// </remarks>
[TestFixtureSource(nameof(Fixtures))]
public class JsonBinaryBlobConformance : FormatSerializerConformance<BinaryBlobSchema>
{
  public static IEnumerable<string> Fixtures => new[] { "Flat/BinaryBlob/rows.json" };

  public JsonBinaryBlobConformance(string fixturePath) : base(fixturePath) { }

  protected override IFormatSerializer<BinaryBlobSchema> CreateSerializer() =>
    new JsonFormatSerializer<BinaryBlobSchema>();

  // Records auto-generate equality based on member-by-member Equals; for byte[] that's
  // reference equality. The kit-level fixture round-trip needs structural equality for
  // the Payload bytes, so override the comparer.
  protected override IEqualityComparer<BinaryBlobSchema> RowComparer =>
    new BinaryBlobSchemaComparer();

  private sealed class BinaryBlobSchemaComparer : IEqualityComparer<BinaryBlobSchema>
  {
    public bool Equals(BinaryBlobSchema? x, BinaryBlobSchema? y)
    {
      if (ReferenceEquals(x, y)) return true;
      if (x is null || y is null) return false;
      return x.Id == y.Id
        && x.Label == y.Label
        && x.Payload.AsSpan().SequenceEqual(y.Payload);
    }

    public int GetHashCode(BinaryBlobSchema obj) => HashCode.Combine(obj.Id, obj.Label);
  }
}

// ── Nested fixtures ─────────────────────────────────────────────────────────

[TestFixtureSource(nameof(Fixtures))]
public class JsonNestedSimpleConformance : FormatSerializerConformance<NestedSimpleSchema>
{
  public static IEnumerable<string> Fixtures => new[] { "Nested/Simple/rows.json" };

  public JsonNestedSimpleConformance(string fixturePath) : base(fixturePath) { }

  protected override IFormatSerializer<NestedSimpleSchema> CreateSerializer() =>
    new JsonFormatSerializer<NestedSimpleSchema>();

  protected override Func<FormatRowFeatures, bool>? RequiredFeatures =>
    f => f.SupportsNested;
}

[TestFixtureSource(nameof(Fixtures))]
public class JsonNestedOptionalConformance : FormatSerializerConformance<NestedOptionalSchema>
{
  public static IEnumerable<string> Fixtures => new[] { "Nested/Optional/rows.json" };

  public JsonNestedOptionalConformance(string fixturePath) : base(fixturePath) { }

  protected override IFormatSerializer<NestedOptionalSchema> CreateSerializer() =>
    new JsonFormatSerializer<NestedOptionalSchema>();

  protected override Func<FormatRowFeatures, bool>? RequiredFeatures =>
    f => f.SupportsNested;
}

[TestFixtureSource(nameof(Fixtures))]
public class JsonNestedArrayConformance : FormatSerializerConformance<NestedArraySchema>
{
  public static IEnumerable<string> Fixtures => new[] { "Nested/Array/rows.json" };

  public JsonNestedArrayConformance(string fixturePath) : base(fixturePath) { }

  protected override IFormatSerializer<NestedArraySchema> CreateSerializer() =>
    new JsonFormatSerializer<NestedArraySchema>();

  protected override Func<FormatRowFeatures, bool>? RequiredFeatures =>
    f => f.SupportsNested;

  // Same array-equality concern as BinaryBlob — records use reference equality for
  // string[] members; sequence-compare them.
  protected override IEqualityComparer<NestedArraySchema> RowComparer =>
    new NestedArraySchemaComparer();

  private sealed class NestedArraySchemaComparer : IEqualityComparer<NestedArraySchema>
  {
    public bool Equals(NestedArraySchema? x, NestedArraySchema? y)
    {
      if (ReferenceEquals(x, y)) return true;
      if (x is null || y is null) return false;
      return x.Name == y.Name && x.Tags.AsSpan().SequenceEqual(y.Tags);
    }

    public int GetHashCode(NestedArraySchema obj) => HashCode.Combine(obj.Name, obj.Tags.Length);
  }
}

[TestFixtureSource(nameof(Fixtures))]
public class JsonNestedIScalarConformance : FormatSerializerConformance<NestedIScalarSchema>
{
  public static IEnumerable<string> Fixtures => new[] { "Nested/IScalar/rows.json" };

  public JsonNestedIScalarConformance(string fixturePath) : base(fixturePath) { }

  protected override IFormatSerializer<NestedIScalarSchema> CreateSerializer() =>
    new JsonFormatSerializer<NestedIScalarSchema>();

  protected override Func<FormatRowFeatures, bool>? RequiredFeatures =>
    f => f.SupportsNested && f.SupportsIScalar;
}
