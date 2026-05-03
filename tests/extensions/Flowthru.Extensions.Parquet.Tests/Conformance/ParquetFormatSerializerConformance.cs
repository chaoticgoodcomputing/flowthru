using Flowthru.Core.Data.Capabilities;
using Flowthru.Core.Data.Storage;
using Flowthru.Core.Data.Storage.Format;
using Flowthru.Tests.Kits.Format;
using Flowthru.Tests.Kits.Schemas;

namespace Flowthru.Extensions.Parquet.Tests.Conformance;

/// <summary>
/// Conformance for <see cref="ParquetFormatSerializer{TRow}"/> against
/// <see cref="TraditionalSchema"/>.
/// </summary>
[TestFixtureSource(nameof(Fixtures))]
public class ParquetTraditionalSchemaConformance : FormatSerializerConformance<TraditionalSchema>
{
  public static IEnumerable<string> Fixtures => new[] { "Flat/Simple/rows.json" };

  public ParquetTraditionalSchemaConformance(string fixturePath) : base(fixturePath) { }

  protected override IFormatSerializer<TraditionalSchema> CreateSerializer() =>
    new ParquetFormatSerializer<TraditionalSchema>();
}

/// <summary>
/// Conformance for <see cref="ParquetFormatSerializer{TRow}"/> against
/// <see cref="RequiredMembersSchema"/> — exercises the activator's slow path for
/// <c>required</c> members through the round-trip.
/// </summary>
[TestFixtureSource(nameof(Fixtures))]
public class ParquetRequiredMembersConformance : FormatSerializerConformance<RequiredMembersSchema>
{
  public static IEnumerable<string> Fixtures => new[] { "Flat/RequiredMembers/rows.json" };

  public ParquetRequiredMembersConformance(string fixturePath) : base(fixturePath) { }

  protected override IFormatSerializer<RequiredMembersSchema> CreateSerializer() =>
    new ParquetFormatSerializer<RequiredMembersSchema>();
}

/// <summary>
/// Conformance for <see cref="ParquetFormatSerializer{TRow}"/> against
/// <see cref="CheckStatusSchema"/>. Verifies Parquet's binary encoding round-trips
/// <c>[SerializedEnum]</c>-decorated enum values. Phase 2 cross-extension scenario.
/// </summary>
[TestFixtureSource(nameof(Fixtures))]
public class ParquetCheckStatusConformance : FormatSerializerConformance<CheckStatusSchema>
{
  public static IEnumerable<string> Fixtures => new[] { "Flat/SerializedEnum/rows.json" };

  public ParquetCheckStatusConformance(string fixturePath) : base(fixturePath) { }

  protected override IFormatSerializer<CheckStatusSchema> CreateSerializer() =>
    new ParquetFormatSerializer<CheckStatusSchema>();
}

/// <summary>
/// Conformance for <see cref="ParquetFormatSerializer{TRow}"/> against
/// <see cref="MultiEnumSchema"/>.
/// </summary>
[TestFixtureSource(nameof(Fixtures))]
public class ParquetMultiEnumConformance : FormatSerializerConformance<MultiEnumSchema>
{
  public static IEnumerable<string> Fixtures => new[] { "Flat/MultiEnum/rows.json" };

  public ParquetMultiEnumConformance(string fixturePath) : base(fixturePath) { }

  protected override IFormatSerializer<MultiEnumSchema> CreateSerializer() =>
    new ParquetFormatSerializer<MultiEnumSchema>();
}

/// <summary>
/// Conformance for <see cref="ParquetFormatSerializer{TRow}"/> against
/// <see cref="MixedRequirementsSchema"/> — exercises required-identity members with
/// optional metadata fields through Parquet's typed-DTO synthesis path.
/// </summary>
[TestFixtureSource(nameof(Fixtures))]
public class ParquetMixedRequirementsConformance : FormatSerializerConformance<MixedRequirementsSchema>
{
  public static IEnumerable<string> Fixtures => new[] { "Flat/MixedRequirements/rows.json" };

  public ParquetMixedRequirementsConformance(string fixturePath) : base(fixturePath) { }

  protected override IFormatSerializer<MixedRequirementsSchema> CreateSerializer() =>
    new ParquetFormatSerializer<MixedRequirementsSchema>();
}

/// <summary>
/// Conformance for <see cref="ParquetFormatSerializer{TRow}"/> against
/// <see cref="PositionalRecordSchema"/> — verifies that primary-constructor records
/// round-trip through Parquet's binary encoding via the activator's slow path.
/// </summary>
[TestFixtureSource(nameof(Fixtures))]
public class ParquetPositionalRecordConformance : FormatSerializerConformance<PositionalRecordSchema>
{
  public static IEnumerable<string> Fixtures => new[] { "Flat/PositionalRecord/rows.json" };

  public ParquetPositionalRecordConformance(string fixturePath) : base(fixturePath) { }

  protected override IFormatSerializer<PositionalRecordSchema> CreateSerializer() =>
    new ParquetFormatSerializer<PositionalRecordSchema>();
}

/// <summary>
/// Conformance for <see cref="ParquetFormatSerializer{TRow}"/> against
/// <see cref="OptionalEnumSchema"/> — verifies nullable enum cells round-trip through
/// Parquet's binary encoding without falling through to a default.
/// </summary>
[TestFixtureSource(nameof(Fixtures))]
public class ParquetOptionalEnumConformance : FormatSerializerConformance<OptionalEnumSchema>
{
  public static IEnumerable<string> Fixtures => new[] { "Flat/OptionalEnum/rows.json" };

  public ParquetOptionalEnumConformance(string fixturePath) : base(fixturePath) { }

  protected override IFormatSerializer<OptionalEnumSchema> CreateSerializer() =>
    new ParquetFormatSerializer<OptionalEnumSchema>();
}

/// <summary>
/// Conformance for <see cref="ParquetFormatSerializer{TRow}"/> against
/// <see cref="IScalarSchema"/>. Parquet declares
/// <see cref="OptOutOfPropertyPlannerAttribute"/> so the planner-driven IScalar
/// integration that CSV / Excel / JSON received in Phase B does not yet apply —
/// this conformance subclass surfaces whether Parquet's existing typed-DTO synthesis
/// happens to handle the wrapper correctly anyway.
/// </summary>
[TestFixtureSource(nameof(Fixtures))]
public class ParquetIScalarConformance : FormatSerializerConformance<IScalarSchema>
{
  public static IEnumerable<string> Fixtures => new[] { "Flat/IScalar/rows.json" };

  public ParquetIScalarConformance(string fixturePath) : base(fixturePath) { }

  protected override IFormatSerializer<IScalarSchema> CreateSerializer() =>
    new ParquetFormatSerializer<IScalarSchema>();

  protected override Func<FormatRowFeatures, bool>? RequiredFeatures =>
    f => f.SupportsIScalar;
}

/// <summary>
/// Conformance for <see cref="ParquetFormatSerializer{TRow}"/> against
/// <see cref="MultiIScalarSchema"/> — multiple distinct IScalar wrappers on one row.
/// </summary>
[TestFixtureSource(nameof(Fixtures))]
public class ParquetMultiIScalarConformance : FormatSerializerConformance<MultiIScalarSchema>
{
  public static IEnumerable<string> Fixtures => new[] { "Flat/MultiIScalar/rows.json" };

  public ParquetMultiIScalarConformance(string fixturePath) : base(fixturePath) { }

  protected override IFormatSerializer<MultiIScalarSchema> CreateSerializer() =>
    new ParquetFormatSerializer<MultiIScalarSchema>();

  protected override Func<FormatRowFeatures, bool>? RequiredFeatures =>
    f => f.SupportsIScalar;
}
