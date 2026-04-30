using System.Collections;

namespace Flowthru.Core.Data;

/// <summary>
/// A typed view over a set of same-schema files within a directory, keyed by full file path.
/// Each entry is one independent unit of <typeparamref name="T"/>; the directory holds N units
/// of the same shape.
/// </summary>
/// <typeparam name="T">
/// The payload type for each file — e.g., <c>byte[]</c> for one binary blob per file,
/// <c>IEnumerable&lt;TRow&gt;</c> for one row-collection per file (CSV-style).
/// </typeparam>
/// <remarks>
/// <para>
/// <strong>This is not a partitioning primitive.</strong> Each entry represents an
/// independent file; <see cref="Directory{T}"/> intentionally exposes no fan-in helpers
/// (no <c>SelectMany</c>-flavoured surface). If you need to chunk one logical dataset
/// across multiple files, do that in a step before write and reassemble in a step after
/// read — keep the partition logic in the domain, not the storage shape.
/// </para>
/// <para>
/// Keys are full file paths so subdirectory nesting and glob-pattern reads (<c>**/*.csv</c>)
/// remain open as a future extension. The storage adapter knows the directory root and the
/// extension; consumers see an opaque <see cref="IReadOnlyDictionary{TKey, TValue}"/>.
/// </para>
/// </remarks>
public sealed class Directory<T> : IReadOnlyDictionary<string, T>
{
  private readonly Dictionary<string, T> _entries;

  /// <summary>
  /// Wraps an existing dictionary as a typed <see cref="Directory{T}"/>. The dictionary is
  /// copied so later mutations to <paramref name="entries"/> don't leak through.
  /// </summary>
  public Directory(IDictionary<string, T> entries)
  {
    if (entries is null)
      throw new ArgumentNullException(nameof(entries));
    _entries = new Dictionary<string, T>(entries, StringComparer.Ordinal);
  }

  /// <summary>
  /// Builds a <see cref="Directory{T}"/> from a sequence of (path, payload) pairs. Duplicate
  /// keys throw — the directory is a 1:1 mapping from path to payload.
  /// </summary>
  public Directory(IEnumerable<KeyValuePair<string, T>> entries)
  {
    if (entries is null)
      throw new ArgumentNullException(nameof(entries));
    _entries = new Dictionary<string, T>(StringComparer.Ordinal);
    foreach (var kvp in entries)
      _entries.Add(kvp.Key, kvp.Value);
  }

  /// <summary>An empty directory (zero entries).</summary>
  public static Directory<T> Empty { get; } =
    new Directory<T>(new Dictionary<string, T>(StringComparer.Ordinal));

  /// <inheritdoc/>
  public T this[string key] => _entries[key];

  /// <inheritdoc/>
  public IEnumerable<string> Keys => _entries.Keys;

  /// <inheritdoc/>
  public IEnumerable<T> Values => _entries.Values;

  /// <inheritdoc/>
  public int Count => _entries.Count;

  /// <inheritdoc/>
  public bool ContainsKey(string key) => _entries.ContainsKey(key);

  /// <inheritdoc/>
  public bool TryGetValue(string key, out T value) => _entries.TryGetValue(key, out value!);

  /// <inheritdoc/>
  public IEnumerator<KeyValuePair<string, T>> GetEnumerator() => _entries.GetEnumerator();

  IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
