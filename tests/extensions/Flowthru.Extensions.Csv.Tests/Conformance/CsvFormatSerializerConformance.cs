using Flowthru.Core.Data.Capabilities;
using Flowthru.Core.Data.Storage;
using Flowthru.Core.Data.Storage.Format;
using Flowthru.Tests.Kits.Format;
using Flowthru.Tests.Kits.Schemas;

namespace Flowthru.Extensions.Csv.Tests.Conformance;

/// <summary>
/// Conformance for <see cref="CsvFormatSerializer{TRow}"/> against
/// <see cref="TraditionalSchema"/>.
/// </summary>
[TestFixtureSource(nameof(Fixtures))]
public class CsvTraditionalSchemaConformance : FormatSerializerConformance<TraditionalSchema>
{
  public static IEnumerable<string> Fixtures => new[] { "Flat/Simple/rows.json" };

  public CsvTraditionalSchemaConformance(string fixturePath) : base(fixturePath) { }

  protected override IFormatSerializer<TraditionalSchema> CreateSerializer() =>
    new CsvFormatSerializer<TraditionalSchema>();
}

/// <summary>
/// Conformance for <see cref="CsvFormatSerializer{TRow}"/> against
/// <see cref="RequiredMembersSchema"/> — exercises the activator's slow path for
/// <c>required</c> members through the round-trip.
/// </summary>
[TestFixtureSource(nameof(Fixtures))]
public class CsvRequiredMembersConformance : FormatSerializerConformance<RequiredMembersSchema>
{
  public static IEnumerable<string> Fixtures => new[] { "Flat/RequiredMembers/rows.json" };

  public CsvRequiredMembersConformance(string fixturePath) : base(fixturePath) { }

  protected override IFormatSerializer<RequiredMembersSchema> CreateSerializer() =>
    new CsvFormatSerializer<RequiredMembersSchema>();
}

/// <summary>
/// Conformance for <see cref="CsvFormatSerializer{TRow}"/> against
/// <see cref="CheckStatusSchema"/> — exercises the full <c>[SerializedEnum]</c> chain
/// (<see cref="SerializedEnumCsvConverter{T}"/> + <see cref="SerializedLabelClassMap{T}"/>)
/// end-to-end for CSV. Phase 2 of the extension coverage audit identified this as the
/// kit shape clearing the ≥3-extension threshold.
/// </summary>
[TestFixtureSource(nameof(Fixtures))]
public class CsvCheckStatusConformance : FormatSerializerConformance<CheckStatusSchema>
{
  public static IEnumerable<string> Fixtures => new[] { "Flat/SerializedEnum/rows.json" };

  public CsvCheckStatusConformance(string fixturePath) : base(fixturePath) { }

  protected override IFormatSerializer<CheckStatusSchema> CreateSerializer() =>
    new CsvFormatSerializer<CheckStatusSchema>();
}

/// <summary>
/// Conformance for <see cref="CsvFormatSerializer{TRow}"/> against
/// <see cref="MultiEnumSchema"/> — verifies the enum chain composes correctly when a
/// row references multiple distinct enum types.
/// </summary>
[TestFixtureSource(nameof(Fixtures))]
public class CsvMultiEnumConformance : FormatSerializerConformance<MultiEnumSchema>
{
  public static IEnumerable<string> Fixtures => new[] { "Flat/MultiEnum/rows.json" };

  public CsvMultiEnumConformance(string fixturePath) : base(fixturePath) { }

  protected override IFormatSerializer<MultiEnumSchema> CreateSerializer() =>
    new CsvFormatSerializer<MultiEnumSchema>();
}

/// <summary>
/// Conformance for <see cref="CsvFormatSerializer{TRow}"/> against
/// <see cref="MixedRequirementsSchema"/> — exercises required identity members alongside
/// optional metadata fields (nullable string, nullable int, default-value bool).
/// </summary>
[TestFixtureSource(nameof(Fixtures))]
public class CsvMixedRequirementsConformance : FormatSerializerConformance<MixedRequirementsSchema>
{
  public static IEnumerable<string> Fixtures => new[] { "Flat/MixedRequirements/rows.json" };

  public CsvMixedRequirementsConformance(string fixturePath) : base(fixturePath) { }

  protected override IFormatSerializer<MixedRequirementsSchema> CreateSerializer() =>
    new CsvFormatSerializer<MixedRequirementsSchema>();
}

/// <summary>
/// Conformance for <see cref="CsvFormatSerializer{TRow}"/> against
/// <see cref="PositionalRecordSchema"/> — exercises positional (primary-constructor)
/// records and the activator's slow path for non-default-constructible types.
/// </summary>
/// <remarks>
/// Phase B2 closed the previous CsvHelper positional-record gap: the migrated
/// <c>SerializedLabelClassMap&lt;T&gt;</c> detects types lacking a parameterless
/// constructor and registers <c>ParameterMap</c> bindings against the primary
/// constructor's parameters. The planner provides the per-property metadata; the CSV
/// class map consumes it for both the Map (parameterless-ctor) and Parameter (positional)
/// flows.
/// </remarks>
[TestFixtureSource(nameof(Fixtures))]
public class CsvPositionalRecordConformance : FormatSerializerConformance<PositionalRecordSchema>
{
  public static IEnumerable<string> Fixtures => new[] { "Flat/PositionalRecord/rows.json" };

  public CsvPositionalRecordConformance(string fixturePath) : base(fixturePath) { }

  protected override IFormatSerializer<PositionalRecordSchema> CreateSerializer() =>
    new CsvFormatSerializer<PositionalRecordSchema>();
}

/// <summary>
/// Conformance for <see cref="CsvFormatSerializer{TRow}"/> against
/// <see cref="OptionalEnumSchema"/> — verifies that nullable enum fields round-trip
/// correctly when the cell value is empty/null in addition to the standard mapped values.
/// </summary>
[TestFixtureSource(nameof(Fixtures))]
public class CsvOptionalEnumConformance : FormatSerializerConformance<OptionalEnumSchema>
{
  public static IEnumerable<string> Fixtures => new[] { "Flat/OptionalEnum/rows.json" };

  public CsvOptionalEnumConformance(string fixturePath) : base(fixturePath) { }

  protected override IFormatSerializer<OptionalEnumSchema> CreateSerializer() =>
    new CsvFormatSerializer<OptionalEnumSchema>();
}

/// <summary>
/// Conformance for <see cref="CsvFormatSerializer{TRow}"/> against
/// <see cref="IScalarSchema"/> — verifies that user-defined NewType wrappers
/// (<see cref="CustomerId"/>) round-trip through CsvHelper via the planner-driven
/// <c>IScalarCsvConverter</c>.
/// </summary>
[TestFixtureSource(nameof(Fixtures))]
public class CsvIScalarConformance : FormatSerializerConformance<IScalarSchema>
{
  public static IEnumerable<string> Fixtures => new[] { "Flat/IScalar/rows.json" };

  public CsvIScalarConformance(string fixturePath) : base(fixturePath) { }

  protected override IFormatSerializer<IScalarSchema> CreateSerializer() =>
    new CsvFormatSerializer<IScalarSchema>();

  protected override Func<FormatRowFeatures, bool>? RequiredFeatures =>
    f => f.SupportsIScalar;
}

/// <summary>
/// Conformance for <see cref="CsvFormatSerializer{TRow}"/> against
/// <see cref="MultiIScalarSchema"/> — exercises multiple distinct IScalar wrapper types
/// (string-, int-, and Guid-backed) on the same row, verifying the planner emits
/// distinct converter wirings per binding.
/// </summary>
[TestFixtureSource(nameof(Fixtures))]
public class CsvMultiIScalarConformance : FormatSerializerConformance<MultiIScalarSchema>
{
  public static IEnumerable<string> Fixtures => new[] { "Flat/MultiIScalar/rows.json" };

  public CsvMultiIScalarConformance(string fixturePath) : base(fixturePath) { }

  protected override IFormatSerializer<MultiIScalarSchema> CreateSerializer() =>
    new CsvFormatSerializer<MultiIScalarSchema>();

  protected override Func<FormatRowFeatures, bool>? RequiredFeatures =>
    f => f.SupportsIScalar;
}
