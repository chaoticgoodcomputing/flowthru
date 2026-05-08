namespace Flowthru.Data.Storage;

/// <summary>
/// Capability matrix for a storage implementation. Defaults represent
/// filesystem-file baseline behavior; constraints narrow it (e.g.,
/// read-only, non-persistent) and capabilities widen it (e.g., streamable,
/// transactional).
/// </summary>
/// <remarks>
/// <para>
/// Adapter authors declare these traits to describe what their storage
/// medium intrinsically supports. Pre-flight uses the traits to fail fast
/// when a flow attempts an operation the storage cannot satisfy
/// (e.g., writing to a read-only source).
/// </para>
/// <para>
/// Catalog authors can further constrain an adapter's traits via
/// <c>Item.Constrain()</c> at catalog construction time. Constraints can
/// only tighten — never loosen — relative to what the adapter declared
/// (one-way ratchet).
/// </para>
/// </remarks>
public record StorageTraits
{
  // ── Constraints (narrow from baseline = filesystem file) ──

  /// <summary>Can data be read from this source? Default <c>true</c>.</summary>
  public bool CanRead { get; init; } = true;

  /// <summary>Can data be written to this source? Default <c>true</c>.</summary>
  public bool CanWrite { get; init; } = true;

  /// <summary>Does data survive across pipeline runs? Default <c>true</c>.</summary>
  public bool IsPersistent { get; init; } = true;

  // ── Capabilities (widen beyond baseline) ──

  /// <summary>Can data be lazily streamed without full materialization? Default <c>false</c>.</summary>
  public bool CanStream { get; init; } = false;

  /// <summary>Can data be appended without replacing existing data? Default <c>false</c>.</summary>
  public bool CanAppend { get; init; } = false;

  /// <summary>Are writes atomic (all-or-nothing)? Default <c>false</c>.</summary>
  public bool IsTransactional { get; init; } = false;
}
