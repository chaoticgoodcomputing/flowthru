namespace Flowthru.Data.Storage;

/// <summary>
/// Optional capability — an adapter or medium implementing this
/// interface declares that the item it backs participates in
/// Flowthru's cache plan. Presence of the interface is the opt-in;
/// absence means the consuming step is uncacheable. The returned
/// fingerprint must satisfy three properties:
///
/// 1. <b>Stable.</b> Repeated calls without intervening state change
///    return the same value.
/// 2. <b>Sensitive.</b> Any change to the medium's content (or to
///    anything observable through a <c>Load()</c> call) changes the
///    fingerprint.
/// 3. <b>Cheap.</b> Derivable without loading the data. Storage
///    metadata (mtime, size, ETag, <c>MAX(updated_at)</c>) is
///    appropriate; streaming the full content is not.
/// </summary>
/// <remarks>
/// <para>
/// Implementations should NOT throw on transient errors — they
/// should return a <see cref="Validation.Runtime.RuntimeError"/>
/// through the <see cref="FlowIO{A}"/> failure channel so the cache
/// plan can record "fingerprint unknown" and treat the dependent
/// step as a cache miss without aborting pre-flight.
/// </para>
/// <para>
/// The interface lives at the storage-adapter / medium layer
/// (alongside <see cref="IHasEfficientCount"/>), not at the
/// <see cref="Flowthru.Data.Catalog.IItem{T}"/> layer. The framework
/// discovers the capability via
/// <see cref="Flowthru.Data.Catalog.IItem.TryGetFingerprint"/>,
/// which delegates to the underlying adapter (or chained mediums)
/// at runtime.
/// </para>
/// <para>
/// In-memory adapters deliberately do not implement this interface:
/// they have no cross-run identity. Step authors who want a flow to
/// participate in caching must wire fingerprintable adapters (file,
/// HTTP, EFCore with a fingerprint column, parquet, directory).
/// </para>
/// </remarks>
public interface ISupportsFingerprint
{
  /// <summary>
  /// Compute the current fingerprint for the backing medium.
  /// </summary>
  /// <remarks>
  /// Returns a <see cref="FlowIO{A}"/> wrapping a hex-encoded string
  /// digest (typically SHA-256 over metadata). Failures surface
  /// through the FlowIO failure channel as
  /// <see cref="Validation.Runtime.RuntimeError"/>; callers treat
  /// "fingerprint unknown" as a cache miss, not a pre-flight abort.
  /// </remarks>
  FlowIO<string> Fingerprint();
}
