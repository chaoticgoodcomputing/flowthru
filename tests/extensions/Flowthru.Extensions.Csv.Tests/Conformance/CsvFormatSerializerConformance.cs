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
