namespace Flowthru.Data.Storage;

/// <summary>
/// Capability seam for byte-level addressing: an adapter or medium
/// implementing this interface can say <em>where its bytes live</em> — a
/// local file path or a remote URI plus access handoff — without loading
/// a single row. This is the seam a consumer that reads storage natively
/// (an embedded engine, a bulk copier) uses to reach an item's bytes
/// directly, the source that <c>IItem&lt;T&gt;.LocateBytes()</c> wraps.
/// </summary>
/// <remarks>
/// <para>
/// Only storage whose backend is genuinely byte-shaped can honor the
/// capability: file and object mediums are addressable; direct adapters
/// (EFCore, Sheets, GQL) and in-memory storage have no byte location.
/// <see cref="IsAddressable"/> lets <c>LocateBytes()</c> callers reject
/// non-addressable storage at wire-up rather than silently materialising
/// rows through <c>Load()</c>.
/// </para>
/// <para>
/// The interface lives at the storage-adapter / medium layer (alongside
/// <see cref="ISupportsFingerprint"/>): mediums implement it as the
/// opt-in, and <see cref="ComposedStorageAdapter{TContainer, TRow}"/>
/// surfaces the medium's answer up to the item.
/// </para>
/// </remarks>
public interface ISupportsByteLocation
{
  /// <summary>
  /// True when the backing storage is byte-addressable. When false,
  /// <see cref="LocateBytes"/> fails through the <see cref="FlowIO{A}"/>
  /// failure channel.
  /// </summary>
  bool IsAddressable { get; }

  /// <summary>
  /// Resolve where this storage's bytes live. Nothing is contacted until
  /// the effect runs; running it resolves the location and — for a remote
  /// location — mints the access handoff through the medium's gateway.
  /// </summary>
  /// <remarks>
  /// The location describes where the bytes live <em>or would land</em>:
  /// a write target is addressable before anything has been written, so
  /// implementations must not fail on absence — existence stays
  /// <see cref="IStorageAdapter{T}.Exists"/>'s question. Failures (a
  /// non-addressable backend, an unresolvable credential chain) surface
  /// as <see cref="Validation.Runtime.RuntimeError"/> through the
  /// <see cref="FlowIO{A}"/> failure channel; nothing throws.
  /// </remarks>
  FlowIO<ByteLocation> LocateBytes();
}
