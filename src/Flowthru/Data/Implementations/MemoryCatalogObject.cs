namespace Flowthru.Data.Implementations;

/// <summary>
/// In-memory catalog entry for transient singleton object storage.
/// </summary>
/// <typeparam name="T">The type of the singleton object to store</typeparam>
/// <remarks>
/// <para>
/// <strong>New in v0.2.0:</strong> This class provides in-memory storage for singleton objects.
/// Previously, singletons were stored using MemoryCatalogEntry&lt;IEnumerable&lt;T&gt;&gt; which
/// required awkward wrapping.
/// </para>
/// <para>
/// <strong>Use Cases:</strong>
/// - ML models in memory during training/evaluation (LinearRegressionModel, ITransformer)
/// - Configuration objects during pipeline execution (ModelOptions, PipelineConfig)
/// - Aggregated metrics (ModelMetrics, PerformanceReport)
/// - Test objects that don't require file I/O
/// </para>
/// <para>
/// For collections of entities, use <see cref="MemoryCatalogDataset{T}"/>.
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
public class MemoryCatalogObject<T> : CatalogObjectBase<T>
{
  private T? _data;
  private bool _hasData;
  private readonly object _lock = new();

  /// <summary>
  /// Creates a new in-memory catalog object.
  /// </summary>
  /// <param name="key">Unique identifier for this catalog object</param>
  public MemoryCatalogObject(string key) : base(key)
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
            $"Cannot load from memory catalog object '{Key}' - no data has been saved yet");
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
