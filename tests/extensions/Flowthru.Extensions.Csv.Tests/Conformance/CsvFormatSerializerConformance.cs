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
