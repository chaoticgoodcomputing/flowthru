using System.Collections.Concurrent;

namespace Flowthru.Data;

/// <summary>
/// Central registry for all catalog entries in a Flowthru project.
/// Provides string-based key access to typed catalog entries.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Design Pattern:</strong> Registry Pattern - maintains a centralized collection
/// of catalog entries accessible by string keys.
/// </para>
/// <para>
/// <strong>Thread Safety:</strong> This class uses ConcurrentDictionary and is thread-safe
/// for registration and retrieval operations.
/// </para>
/// <para>
/// <strong>Usage:</strong> Typically constructed via DataCatalogBuilder, though it can
/// be instantiated directly if needed.
/// </para>
/// </remarks>
public class DataCatalog {
  private readonly ConcurrentDictionary<string, ICatalogEntry> _entries = new();

  /// <summary>
  /// Registers a catalog entry with the specified key.
  /// </summary>
  /// <param name="key">Unique identifier for the catalog entry</param>
  /// <param name="entry">The catalog entry to register</param>
  /// <exception cref="ArgumentException">
  /// Thrown if a catalog entry with the same key is already registered
  /// </exception>
  public void Register(string key, ICatalogEntry entry) {
    if (!_entries.TryAdd(key, entry)) {
      throw new ArgumentException(
          $"Catalog entry with key '{key}' is already registered", nameof(key));
    }
  }

  /// <summary>
  /// Retrieves a catalog entry by key.
  /// </summary>
  /// <param name="key">The key of the catalog entry to retrieve</param>
  /// <returns>The catalog entry (untyped)</returns>
  /// <exception cref="KeyNotFoundException">
  /// Thrown if no catalog entry with the specified key exists
  /// </exception>
  public ICatalogEntry Get(string key) {
    if (!_entries.TryGetValue(key, out var entry)) {
      throw new KeyNotFoundException(
          $"No catalog entry found with key '{key}'");
    }

    return entry;
  }

  /// <summary>
  /// Retrieves an untyped catalog entry by key.
  /// </summary>
  /// <param name="key">The key of the catalog entry to retrieve</param>
  /// <returns>The catalog entry (untyped)</returns>
  /// <exception cref="KeyNotFoundException">
  /// Thrown if no catalog entry with the specified key exists
  /// </exception>
  /// <remarks>
  /// Used internally by the mapping layer for reflection-based operations.
  /// Prefer the strongly-typed Get&lt;T&gt;() method when possible.
  /// </remarks>
  public ICatalogEntry GetUntyped(string key) {
    if (!_entries.TryGetValue(key, out var entry)) {
      throw new KeyNotFoundException(
          $"No catalog entry found with key '{key}'");
    }

    return entry;
  }

  /// <summary>
  /// Checks if a catalog entry with the specified key exists.
  /// </summary>
  /// <param name="key">The key to check</param>
  /// <returns>True if a catalog entry with the key exists, false otherwise</returns>
  public bool Contains(string key) => _entries.ContainsKey(key);

  /// <summary>
  /// Gets all registered catalog entry keys.
  /// </summary>
  public IEnumerable<string> Keys => _entries.Keys;

  /// <summary>
  /// Gets all registered catalog entries.
  /// </summary>
  public IEnumerable<ICatalogEntry> Entries => _entries.Values;
}
