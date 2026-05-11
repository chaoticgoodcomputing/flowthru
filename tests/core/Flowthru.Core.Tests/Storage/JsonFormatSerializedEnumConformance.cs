using Flowthru.Data.Storage;
using Flowthru.Tests.Kits.Storage;

namespace Flowthru.Core.Tests.Storage;

/// <summary>
/// Runs the shared <see cref="SerializedEnumConformance"/> suite against
/// <see cref="JsonFormatSerializer{TRow}"/>. Every format adapter that
/// opts into <see cref="Flowthru.Data.Schema.SerializedEnumAttribute"/>
/// support inherits an identical fixture in its own test project, so
/// schema-level enum behavior is guaranteed identical across formats.
/// </summary>
public sealed class JsonFormatSerializedEnumConformance : SerializedEnumConformance
{
  /// <inheritdoc/>
  protected override IFormatSerializer<KitRow> CreateSerializer() =>
    new JsonFormatSerializer<KitRow>();
}
