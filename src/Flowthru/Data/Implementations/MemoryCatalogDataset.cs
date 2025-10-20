using System.Collections.Concurrent;

namespace Flowthru.Data.Implementations;

/// <summary>
/// In-memory catalog entry for transient dataset storage (collections).
/// </summary>
/// <typeparam name="T">The type of individual items in the dataset (NOT IEnumerable&lt;T&gt;)</typeparam>
/// <remarks>
/// <para>
/// <strong>Breaking Change (v0.2.0):</strong> This class now extends CatalogDatasetBase&lt;T&gt; instead of CatalogEntryBase&lt;IEnumerable&lt;T&gt;&gt;.
/// Previously: <c>MemoryCatalogEntry&lt;IEnumerable&lt;Company&gt;&gt;</c>
/// Now: <c>MemoryCatalogEntry&lt;Company&gt;</c>
/// </para>
/// <para>
/// <strong>Use Cases:</strong>
/// - Intermediate pipeline datasets that don't need persistence
/// - Test data that doesn't require file I/O
/// - Temporary results between pipeline stages
/// - Collections of entities (rows, records, items)
/// </para>
/// <para>
/// For singleton objects (ML models, metrics), use <see cref="MemoryCatalogObject{T}"/>.
/// </para>
/// <para>
/// <strong>Thread Safety:</strong> This implementation is thread-safe for concurrent
/// Save() and Load() operations.
/// </para>
/// <para>
/// <strong>Lifetime:</strong> Data persists only for the lifetime of this instance.
/// Data is lost when the application terminates.
/// </para>
/// </remarks>
public class MemoryCatalogDataset<T> : CatalogDatasetBase<T>
{
  private IEnumerable<T>? _data;
  private bool _hasData;
  private readonly object _lock = new();

  /// <summary>
  /// Creates a new in-memory catalog entry.
  /// </summary>
  /// <param name="key">Unique identifier for this catalog entry</param>
  public MemoryCatalogDataset(string key) : base(key)
  {
  }

  /// <inheritdoc/>
  public override Task<IEnumerable<T>> Load()
  {
    lock (_lock)
    {
      if (!_hasData)
      {
        throw new InvalidOperationException(
            $"Cannot load from memory catalog entry '{Key}' - no data has been saved yet");
      }

      return Task.FromResult(_data)!;
    }
  }

  /// <inheritdoc/>
  public override Task Save(IEnumerable<T> data)
  {
    lock (_lock)
    {
      // Materialize the enumerable to avoid deferred execution issues
      _data = data.ToList();
      _hasData = true;
    }

    return Task.CompletedTask;
  }

  /// <inheritdoc/>
  public override Task<bool> Exists()
  {
    lock (_lock)
    {
      return Task.FromResult(_hasData);
    }
  }

  /// <inheritdoc/>
  /// <remarks>
  /// Optimized implementation that doesn't require loading data.
  /// Returns the count of items in the stored collection.
  /// </remarks>
  public override Task<int> GetCountAsync()
  {
    lock (_lock)
    {
      if (!_hasData || _data == null)
        return Task.FromResult(0);

      return Task.FromResult(_data.Count());
    }
  }

  /// <summary>
  /// Clears the stored data.
  /// </summary>
  public void Clear()
  {
    lock (_lock)
    {
      _data = default;
      _hasData = false;
    }
  }
}
