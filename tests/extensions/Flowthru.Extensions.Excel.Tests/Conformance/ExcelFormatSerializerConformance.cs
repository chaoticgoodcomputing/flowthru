using Flowthru.Core.Data.Capabilities;
using Flowthru.Core.Data.Storage;
using Flowthru.Core.Data.Storage.Format;
using Flowthru.Tests.Kits.Format;
using Flowthru.Tests.Kits.Schemas;

namespace Flowthru.Extensions.Excel.Tests.Conformance;

/// <summary>
/// Conformance for <see cref="ExcelFormatSerializer{TRow}"/>.
/// </summary>
/// <remarks>
/// Excel is a read-only format (<c>Traits.CanWrite = false</c>). The kit's round-trip test
/// passes vacuously; the contractual obligations covered are
/// <see cref="IFormatSerializer{TRow}.GetPropertyMappingConfiguration"/> and the
/// trait-honesty assertion. Read-path correctness is exercised by
/// <c>ExcelFormatSerializerTests</c>, which builds in-memory .xlsx via ClosedXML.
/// </remarks>
[TestFixtureSource(nameof(Fixtures))]
public class ExcelTraditionalSchemaConformance : FormatSerializerConformance<TraditionalSchema>
{
  public static IEnumerable<string> Fixtures => new[] { "Flat/Simple/rows.json" };

  public ExcelTraditionalSchemaConformance(string fixturePath) : base(fixturePath) { }

  protected override IFormatSerializer<TraditionalSchema> CreateSerializer() =>
    new ExcelFormatSerializer<TraditionalSchema>(sheetName: "Sheet1");
}

/// <summary>
/// Conformance for <see cref="ExcelFormatSerializer{TRow}"/> against
/// <see cref="CheckStatusSchema"/>. Excel is read-only (Traits.CanWrite = false),
/// so the round-trip test passes vacuously; the contractual test that exercises is
/// <see cref="FormatSerializerConformance{TRow}.GetPropertyMappingConfiguration_ReturnsNonNull"/>,
/// which asserts the serializer can construct a property mapping for an enum-bearing
/// schema without throwing. Phase 2 cross-extension scenario.
/// </summary>
[TestFixtureSource(nameof(Fixtures))]
public class ExcelCheckStatusConformance : FormatSerializerConformance<CheckStatusSchema>
{
  public static IEnumerable<string> Fixtures => new[] { "Flat/SerializedEnum/rows.json" };

  public ExcelCheckStatusConformance(string fixturePath) : base(fixturePath) { }

  protected override IFormatSerializer<CheckStatusSchema> CreateSerializer() =>
    new ExcelFormatSerializer<CheckStatusSchema>(sheetName: "Sheet1");
}

/// <summary>
/// Conformance for <see cref="ExcelFormatSerializer{TRow}"/> against
/// <see cref="MultiEnumSchema"/>.
/// </summary>
[TestFixtureSource(nameof(Fixtures))]
public class ExcelMultiEnumConformance : FormatSerializerConformance<MultiEnumSchema>
{
  public static IEnumerable<string> Fixtures => new[] { "Flat/MultiEnum/rows.json" };

  public ExcelMultiEnumConformance(string fixturePath) : base(fixturePath) { }

  protected override IFormatSerializer<MultiEnumSchema> CreateSerializer() =>
    new ExcelFormatSerializer<MultiEnumSchema>(sheetName: "Sheet1");
}

/// <summary>
/// Conformance for <see cref="ExcelFormatSerializer{TRow}"/> against
/// <see cref="MixedRequirementsSchema"/>. Excel is read-only; the round-trip test passes
/// vacuously and the contractual obligation that fires is the property-mapping
/// configuration check — confirming the serializer can construct a property mapping for
/// a schema mixing required-identity and optional-metadata fields without throwing.
/// </summary>
[TestFixtureSource(nameof(Fixtures))]
public class ExcelMixedRequirementsConformance : FormatSerializerConformance<MixedRequirementsSchema>
{
  public static IEnumerable<string> Fixtures => new[] { "Flat/MixedRequirements/rows.json" };

  public ExcelMixedRequirementsConformance(string fixturePath) : base(fixturePath) { }

  protected override IFormatSerializer<MixedRequirementsSchema> CreateSerializer() =>
    new ExcelFormatSerializer<MixedRequirementsSchema>(sheetName: "Sheet1");
}

/// <summary>
/// Conformance for <see cref="ExcelFormatSerializer{TRow}"/> against
/// <see cref="PositionalRecordSchema"/>. Excel is read-only; this exercises the
/// property-mapping path for a positional (primary-constructor) record.
/// </summary>
[TestFixtureSource(nameof(Fixtures))]
public class ExcelPositionalRecordConformance : FormatSerializerConformance<PositionalRecordSchema>
{
  public static IEnumerable<string> Fixtures => new[] { "Flat/PositionalRecord/rows.json" };

  public ExcelPositionalRecordConformance(string fixturePath) : base(fixturePath) { }

  protected override IFormatSerializer<PositionalRecordSchema> CreateSerializer() =>
    new ExcelFormatSerializer<PositionalRecordSchema>(sheetName: "Sheet1");
}

/// <summary>
/// Conformance for <see cref="ExcelFormatSerializer{TRow}"/> against
/// <see cref="OptionalEnumSchema"/>. Excel is read-only; this exercises property-mapping
/// for a schema with a nullable enum field.
/// </summary>
[TestFixtureSource(nameof(Fixtures))]
public class ExcelOptionalEnumConformance : FormatSerializerConformance<OptionalEnumSchema>
{
  public static IEnumerable<string> Fixtures => new[] { "Flat/OptionalEnum/rows.json" };

  public ExcelOptionalEnumConformance(string fixturePath) : base(fixturePath) { }

  protected override IFormatSerializer<OptionalEnumSchema> CreateSerializer() =>
    new ExcelFormatSerializer<OptionalEnumSchema>(sheetName: "Sheet1");
}

/// <summary>
/// Conformance for <see cref="ExcelFormatSerializer{TRow}"/> against
/// <see cref="IScalarSchema"/>. Excel is read-only; the round-trip test passes vacuously
/// and the property-mapping check exercises the planner-driven IScalar binding emission.
/// </summary>
[TestFixtureSource(nameof(Fixtures))]
public class ExcelIScalarConformance : FormatSerializerConformance<IScalarSchema>
{
  public static IEnumerable<string> Fixtures => new[] { "Flat/IScalar/rows.json" };

  public ExcelIScalarConformance(string fixturePath) : base(fixturePath) { }

  protected override IFormatSerializer<IScalarSchema> CreateSerializer() =>
    new ExcelFormatSerializer<IScalarSchema>(sheetName: "Sheet1");

  protected override Func<FormatRowFeatures, bool>? RequiredFeatures =>
    f => f.SupportsIScalar;
}

/// <summary>
/// Conformance for <see cref="ExcelFormatSerializer{TRow}"/> against
/// <see cref="MultiIScalarSchema"/>.
/// </summary>
[TestFixtureSource(nameof(Fixtures))]
public class ExcelMultiIScalarConformance : FormatSerializerConformance<MultiIScalarSchema>
{
  public static IEnumerable<string> Fixtures => new[] { "Flat/MultiIScalar/rows.json" };

  public ExcelMultiIScalarConformance(string fixturePath) : base(fixturePath) { }

  protected override IFormatSerializer<MultiIScalarSchema> CreateSerializer() =>
    new ExcelFormatSerializer<MultiIScalarSchema>(sheetName: "Sheet1");

  protected override Func<FormatRowFeatures, bool>? RequiredFeatures =>
    f => f.SupportsIScalar;
}
