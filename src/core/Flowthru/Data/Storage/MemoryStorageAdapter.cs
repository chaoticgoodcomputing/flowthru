using Flowthru.Effects;

namespace Flowthru.Data.Storage;

/// <summary>
/// Direct memory storage adapter that bypasses serialization.
/// </summary>
/// <typeparam name="T">The data type to store in memory</typeparam>
/// <remarks>
/// <para>
/// <strong>Design Rationale:</strong> Memory storage doesn't need byte serialization
/// since objects stay in-process. This adapter provides direct Load/Save without
/// medium/format/container composition.
/// </para>
/// <para>
/// <strong>Use Cases:</strong>
/// </para>
/// <list type="bullet">
/// <item>Intermediate pipeline data that doesn't need persistence</item>
/// <item>Test data that doesn't require file I/O</item>
/// <item>Temporary results between pipeline stages</item>
/// <item>ML models, metrics, charts, or any ephemeral data</item>
/// </list>
/// <para>
/// <strong>Thread Safety:</strong> Thread-safe for concurrent Load/Save operations
/// </para>
/// <para>
/// <strong>Lifetime:</strong> Data persists only for the lifetime of this instance
/// </para>
/// <para>
/// <strong>Capabilities:</strong>
/// </para>
/// <list type="bullet">
/// <item>ISeedable: false (memory cannot be Layer 0 input)</item>
/// <item>IReadOnly: false</item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // Singleton usage
/// var modelStorage = new MemoryStorageAdapter&lt;LinearRegressionModel&gt;();
/// var modelEntry = new CatalogEntry&lt;LinearRegressionModel&gt;("model", modelStorage);
///
/// // Collection usage
/// var dataStorage = new MemoryStorageAdapter&lt;IEnumerable&lt;FeatureRow&gt;&gt;();
/// var dataEntry = new CatalogEntry&lt;IEnumerable&lt;FeatureRow&gt;&gt;("features", dataStorage);
/// </code>
/// </example>
public sealed class MemoryStorageAdapter<T> : IStorageAdapter<T>
{
  private T? _data;
  private bool _hasData;
  private readonly object _lock = new();

  /// <summary>
  /// Creates a new in-memory storage adapter.
  /// </summary>
  public MemoryStorageAdapter() { }

  /// <summary>
  /// Creates a new in-memory storage adapter with initial data.
  /// </summary>
  /// <param name="initialData">Initial data to store</param>
  public MemoryStorageAdapter(T initialData)
  {
    _data = initialData;
    _hasData = true;
  }

  /// <inheritdoc />
  public FlowIO<T> Load()
  {
    return FlowIO.Lift(() =>
    {
      lock (_lock)
      {
        if (!_hasData)
        {
          throw new InvalidOperationException(
            "Cannot load from memory storage - no data has been saved yet"
          );
        }
        return _data!;
      }
    });
  }

  /// <inheritdoc />
  public FlowIO<FlowUnit> Save(T data)
  {
    return FlowIO.Lift(() =>
    {
      lock (_lock)
      {
        _data = data;
        _hasData = true;
        return FlowUnit.Default;
      }
    });
  }

  /// <inheritdoc />
  public FlowIO<bool> Exists()
  {
    return FlowIO.Lift(() =>
    {
      lock (_lock)
      {
        return _hasData;
      }
    });
  }
}
