using Flowthru.Prelude;

namespace Flowthru.Data.Storage;

/// <summary>
/// Optional capability — a storage adapter (or medium) implementing this
/// interface can hand out its item's content as a provider-native bulk
/// byte stream (e.g. a Postgres binary <c>COPY TO</c>), letting a bulk
/// transfer bypass row-at-a-time marshalling entirely. This is the
/// exporting half of the transfer pairing; <see cref="ISupportsBulkImport"/>
/// is the receiving half.
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
/// can pair without referencing each other: both sides speak this shared
/// vocabulary and match on the identity strings. The framework discovers
/// the capability via
/// <see cref="Flowthru.Data.Catalog.IItem.TryGetBulkExport"/>, which
/// delegates to the underlying adapter.
/// </para>
/// </remarks>
public interface ISupportsBulkExport
{
  /// <summary>
  /// Canonical lowercase identifier of the storage provider whose native
  /// bulk representation this exporter emits (e.g. <c>"postgresql"</c>).
  /// Two endpoints can pair only when their providers are equal.
  /// </summary>
  string BulkProvider { get; }

  /// <summary>
  /// Canonical lowercase identifier of the wire format the export channel
  /// carries (e.g. <c>"pgcopy-binary"</c>). Distinct from
  /// <see cref="BulkProvider"/> so one provider can ship multiple
  /// encodings without breaking the pairing rule.
  /// </summary>
  string BulkWireFormat { get; }

  /// <summary>
  /// Open the readable byte channel carrying this item's content in
  /// <see cref="BulkWireFormat"/>. Called only at transfer runtime by a
  /// native rung — never during negotiation. Failures surface through the
  /// <see cref="FlowIO{A}"/> failure channel; the caller owns the stream
  /// and disposes it when the transfer completes or aborts.
  /// Implementations must treat disposal before end-of-stream as an
  /// abort and cancel any in-flight provider operation.
  /// </summary>
  FlowIO<Stream> OpenBulkExport();
}
