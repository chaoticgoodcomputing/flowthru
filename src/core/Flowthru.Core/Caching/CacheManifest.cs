using Flowthru.Data.Schema;

namespace Flowthru.Caching;

/// <summary>
/// Persisted state of Flowthru's cache plan — a per-node-label map of
/// fingerprints recorded the last time each node ran successfully.
/// </summary>
/// <remarks>
/// <para>
/// <strong>What's a node?</strong> Both steps and catalog items live
/// in the same label namespace from the cache plan's point of view.
/// Step entries record the composite hash of <c>CodeVersion</c> + the
/// rolled-up identities of every input; item entries record the leaf
/// fingerprint the item's storage adapter reported on the previous
/// run. The cache-plan walk reads both kinds to decide whether each
/// step is fresh, stale, or uncacheable.
/// </para>
/// <para>
/// <strong>Schema versioning.</strong>
/// <see cref="SchemaVersion"/> is checked on load. If a process loads
/// a manifest whose version doesn't match
/// <see cref="CacheManifestSchema.CurrentVersion"/>, the framework
/// treats the manifest as empty and re-records every node on the next
/// successful run. No in-place migrations in v1.
/// </para>
/// <para>
/// <strong>Concurrency.</strong> Per-entry
/// <see cref="NodeFingerprint.RecordedAt"/> timestamps allow
/// last-write-wins merge when two runs touch the manifest concurrently.
/// The framework's save path re-loads the on-disk manifest, merges
/// per-entry, and writes the union — no entry from a non-overlapping
/// run is ever dropped.
/// </para>
/// </remarks>
public sealed record CacheManifest(
  int SchemaVersion,
  IReadOnlyDictionary<string, NodeFingerprint> Entries
) : IStructuredSerializable
{
  /// <summary>An empty manifest at the current schema version.</summary>
  public static CacheManifest Empty { get; } =
    new(CacheManifestSchema.CurrentVersion, new Dictionary<string, NodeFingerprint>(StringComparer.Ordinal));

  /// <summary>
  /// True iff this manifest's schema matches
  /// <see cref="CacheManifestSchema.CurrentVersion"/>. Callers that
  /// load a manifest from storage should check this and replace with
  /// <see cref="Empty"/> on mismatch.
  /// </summary>
  /// <remarks>
  /// Modelled as a method, not a property, so the Flowthru JSON
  /// converter (which round-trips every public property) doesn't try
  /// to set a derived getter-only value at deserialization.
  /// </remarks>
  public bool IsCurrentSchema() => SchemaVersion == CacheManifestSchema.CurrentVersion;
}

/// <summary>
/// Schema-version metadata for <see cref="CacheManifest"/>. Bumping
/// the constant invalidates every project's existing cache —
/// intentional v1 stance to avoid building a migration framework
/// before there's evidence the cost is worth it.
/// </summary>
public static class CacheManifestSchema
{
  /// <summary>
  /// The schema version this build of Flowthru understands. Bump on
  /// any structural change to <see cref="CacheManifest"/>,
  /// <see cref="NodeFingerprint"/>, or the composite-hash derivation.
  /// </summary>
  public const int CurrentVersion = 1;
}
