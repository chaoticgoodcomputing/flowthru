using Flowthru.Data.Storage;
using Flowthru.Data.Storage.Csv;
using Flowthru.Tests.Kits.Storage;

namespace Flowthru.Extensions.Csv.Tests;

/// <summary>
/// Runs the shared <see cref="SerializedEnumConformance"/> suite against
/// <see cref="CsvFormatSerializer{TRow}"/>. CSV is a text format, so the
/// full conformance set (round-trip, wire-format inspection, unknown-
/// string failure, undeclared-int failure) applies.
/// </summary>
public sealed class CsvSerializedEnumConformance : SerializedEnumConformance
{
  /// <inheritdoc/>
  protected override IFormatSerializer<KitRow> CreateSerializer() =>
    new CsvFormatSerializer<KitRow>();
}
