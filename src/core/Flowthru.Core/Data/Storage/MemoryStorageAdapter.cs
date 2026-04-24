using Flowthru.Core.Data.Capabilities;
using Flowthru.Core.Effects;

namespace Flowthru.Core.Data.Storage;

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
/// <strong>Storage Traits:</strong>
/// </para>
/// <list type="bullet">
/// <item>IsPersistent: false (data lost when process exits)</item>
/// <item>All other traits use filesystem baseline defaults</item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // Singleton usage
/// var modelStorage = new MemoryStorageAdapter&lt;LinearRegressionModel&gt;();
/// var modelEntry = new Item&lt;LinearRegressionModel&gt;("model", modelStorage);
///
/// // Collection usage
/// var dataStorage = new MemoryStorageAdapter&lt;IEnumerable&lt;FeatureRow&gt;&gt;();
/// var dataEntry = new Item&lt;IEnumerable&lt;FeatureRow&gt;&gt;("features", dataStorage);
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
  public StorageTraits Traits => new StorageTraits { IsPersistent = false };

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

  /// <inheritdoc />
  public FlowIO<Data.Validation.ValidationResult> InspectShallow(int sampleSize)
  {
    return FlowIO.Lift(() =>
    {
      lock (_lock)
      {
        if (!_hasData)
        {
          return Data.Validation.ValidationResult.Failure(
            catalogKey: typeof(T).Name,
            errorType: Data.Validation.ValidationErrorType.NotFound,
            message: $"Memory storage for '{typeof(T).Name}' has no data",
            details: "Data must be saved before it can be loaded"
          );
        }

        return Data.Validation.ValidationResult.Success();
      }
    });
  }

  /// <inheritdoc />
  public FlowIO<Data.Validation.ValidationResult> InspectDeep()
  {
    // For memory storage, deep inspection is equivalent to shallow
    // since all data is already in memory
    return InspectShallow(sampleSize: 0);
  }

  /// <inheritdoc />
  /// <remarks>Memory is always a valid write destination — no pre-conditions to validate.</remarks>
  public FlowIO<Data.Validation.ValidationResult> InspectTarget() =>
    FlowIO.Pure(Data.Validation.ValidationResult.Success());
}
