using System.Collections.Concurrent;

namespace Flowthru.Data.Implementations;

/// <summary>
/// In-memory catalog entry for transient data storage.
/// </summary>
/// <typeparam name="T">The type of data to store</typeparam>
/// <remarks>
/// <para>
/// <strong>Use Cases:</strong>
/// - Intermediate pipeline datasets that don't need persistence
/// - ML models in memory during training/evaluation
/// - Test data that doesn't require file I/O
/// - Temporary results between pipeline stages
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
public class MemoryCatalogEntry<T> : CatalogEntryBase<T>
{
    private T? _data;
    private bool _hasData;
    private readonly object _lock = new();

    /// <summary>
    /// Creates a new in-memory catalog entry.
    /// </summary>
    /// <param name="key">Unique identifier for this catalog entry</param>
    public MemoryCatalogEntry(string key) : base(key)
    {
    }

    /// <inheritdoc/>
    public override Task<T> Load()
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
    public override Task Save(T data)
    {
        lock (_lock)
        {
            _data = data;
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
