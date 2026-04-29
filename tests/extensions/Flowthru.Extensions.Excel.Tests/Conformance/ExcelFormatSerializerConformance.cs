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
