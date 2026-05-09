using System.Collections;

namespace Flowthru.Data.Storage;

/// <summary>
/// Typed view over a set of same-schema files within a directory,
/// keyed by full file path. Each entry is one independent unit of
/// <typeparamref name="T"/>; the directory holds N units of the same
/// shape. Backing container for
/// <see cref="DirectoryStorageAdapter{T}"/> and the
/// <c>ItemFactory.DirectoryOf</c> family of smart constructors.
/// </summary>
/// <typeparam name="T">
/// Payload type for each file — e.g., <c>byte[]</c> for one binary
/// blob per file, <c>IEnumerable&lt;TRow&gt;</c> for one row-collection
/// per file (CSV-style), <c>TDoc</c> for a single document per file.
/// </typeparam>
/// <remarks>
/// <para>
/// <strong>This is not a partitioning primitive.</strong> Each entry
/// represents an independent file; <see cref="DirectoryOf{T}"/>
/// intentionally exposes no fan-in helpers. If you need to chunk one
/// logical dataset across multiple files, do that in a step before
/// write and reassemble in a step after read — keep the partition
/// logic in the domain, not the storage shape.
/// </para>
/// <para>
/// Keys are full file paths so subdirectory nesting and glob-pattern
/// reads (<c>**/*.csv</c>) remain open as a future extension. The
/// storage adapter knows the directory root and the extension;
/// consumers see an opaque <see cref="IReadOnlyDictionary{TKey, TValue}"/>.
/// </para>
/// </remarks>
public sealed class DirectoryOf<T> : IReadOnlyDictionary<string, T>
{
  private readonly Dictionary<string, T> _entries;

  public DirectoryOf(IDictionary<string, T> entries)
  {
    if (entries is null) throw new ArgumentNullException(nameof(entries));
    _entries = new Dictionary<string, T>(entries, StringComparer.Ordinal);
  }

  public DirectoryOf(IEnumerable<KeyValuePair<string, T>> entries)
  {
    if (entries is null) throw new ArgumentNullException(nameof(entries));
    _entries = new Dictionary<string, T>(StringComparer.Ordinal);
    foreach (var kvp in entries) _entries.Add(kvp.Key, kvp.Value);
  }

  public static DirectoryOf<T> Empty { get; } =
    new DirectoryOf<T>(new Dictionary<string, T>(StringComparer.Ordinal));

  public T this[string key] => _entries[key];
  public IEnumerable<string> Keys => _entries.Keys;
  public IEnumerable<T> Values => _entries.Values;
  public int Count => _entries.Count;
  public bool ContainsKey(string key) => _entries.ContainsKey(key);
  public bool TryGetValue(string key, out T value) => _entries.TryGetValue(key, out value!);
  public IEnumerator<KeyValuePair<string, T>> GetEnumerator() => _entries.GetEnumerator();
  IEnumerator IEnumerable.GetEnumerator() => _entries.GetEnumerator();
}
