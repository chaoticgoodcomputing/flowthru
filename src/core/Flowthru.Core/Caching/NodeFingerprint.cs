using Flowthru.Data.Schema;

namespace Flowthru.Caching;

/// <summary>
/// A single entry in a <see cref="CacheManifest"/>: the fingerprint
/// recorded for a step or item label the last time it was successfully
/// produced (steps) or read at fingerprint time (items).
/// </summary>
/// <param name="Value">
/// Opaque hash string. The framework treats this as a black-box
/// identifier — composite hashes for steps, leaf fingerprints for items,
/// both pass through the same field.
/// </param>
/// <param name="RecordedAt">
/// Wall-clock time the entry was recorded. Used to resolve concurrent
/// writes via last-write-wins merge: when two processes touch the same
/// label, the entry with the greater <see cref="RecordedAt"/> survives.
/// </param>
public sealed record NodeFingerprint(string Value, DateTimeOffset RecordedAt) : IStructuredSerializable;
