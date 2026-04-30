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
