using Flowthru.Prelude;

namespace Flowthru.Data.Storage;

/// <summary>
/// Optional capability — a storage adapter (or medium) implementing this
/// interface can receive an item's content as a provider-native bulk byte
/// stream (e.g. a Postgres binary <c>COPY FROM</c>), letting a bulk
/// transfer bypass row-at-a-time marshalling entirely. This is the
/// receiving half of the transfer pairing; <see cref="ISupportsBulkExport"/>
/// is the exporting half.
/// </summary>
/// <remarks>
/// <para>
/// The pair is negotiated at pre-flight by
/// <see cref="Flowthru.Flow.BulkTransferNegotiation"/>: a source and a
/// target are native-compatible iff the source's exporter and the target's
/// importer agree on both <see cref="BulkProvider"/> and
/// <see cref="BulkWireFormat"/>. The identity properties must be pure
/// metadata — negotiation runs in the zero-I/O pre-flight tier, so reading
/// them must touch no socket, file, or database.
/// </para>
/// <para>
/// The interface lives in Core's cross-cutting capability surface
/// (alongside <see cref="ISupportsFingerprint"/>) so any two extensions
/// can pair without referencing each other. The framework discovers the
/// capability via
/// <see cref="Flowthru.Data.Catalog.IItem.TryGetBulkImport"/>, which
/// delegates to the underlying adapter.
/// </para>
/// </remarks>
public interface ISupportsBulkImport
{
  /// <summary>
  /// Canonical lowercase identifier of the storage provider whose native
  /// bulk representation this importer accepts (e.g. <c>"postgresql"</c>).
  /// Two endpoints can pair only when their providers are equal.
  /// </summary>
  string BulkProvider { get; }

  /// <summary>
  /// Canonical lowercase identifier of the wire format the import channel
  /// expects (e.g. <c>"pgcopy-binary"</c>). Distinct from
  /// <see cref="BulkProvider"/> so one provider can ship multiple
  /// encodings without breaking the pairing rule.
  /// </summary>
  string BulkWireFormat { get; }

  /// <summary>
  /// Open the writable byte channel that lands content into this item in
  /// <see cref="BulkWireFormat"/>. Called only at transfer runtime by a
  /// native rung — never during negotiation. Failures surface through the
  /// <see cref="FlowIO{A}"/> failure channel; the caller writes the
  /// exported bytes and disposes the stream, and the implementation must
  /// finalize the import on clean disposal after a complete payload and
  /// abort (e.g. roll back) otherwise.
  /// </summary>
  FlowIO<Stream> OpenBulkImport();
}
