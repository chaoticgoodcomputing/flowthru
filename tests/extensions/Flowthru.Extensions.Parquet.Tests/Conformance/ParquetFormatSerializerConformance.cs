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
