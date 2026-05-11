namespace Flowthru.Data.Storage;

/// <summary>
/// Direct in-memory storage adapter. Bypasses the medium × format ×
/// container composition because in-memory data needs no byte-level
/// serialization; it simply holds the value in a field. Useful for
/// intermediate flow data, test fixtures, ephemeral results.
/// </summary>
/// <typeparam name="T">The data type stored in memory.</typeparam>
/// <remarks>
/// <para>
/// Thread-safe for concurrent <c>Load</c>/<c>Save</c>. Data persists only
/// for the lifetime of the adapter instance — <see cref="StorageTraits.IsPersistent"/>
/// is <c>false</c>.
/// </para>
/// </remarks>
public sealed class MemoryStorageAdapter<T> : IStorageAdapter<T>
{
  private T? _data;
  private bool _hasData;
  private readonly object _lock = new();

  /// <inheritdoc/>
  public StorageTraits Traits { get; } = new() { IsPersistent = false };

  /// <inheritdoc/>
  public FlowIO<T> Load() =>
    FlowIO.Lift(() =>
    {
      lock (_lock)
      {
        if (!_hasData)
        {
          throw new InvalidOperationException(
            "MemoryStorageAdapter has no data — Save() must be called before Load()."
          );
        }
        return _data!;
      }
    });

  /// <inheritdoc/>
  public FlowIO<FlowUnit> Save(T data) =>
    FlowIO.Lift(() =>
    {
      lock (_lock)
      {
        _data = data;
        _hasData = true;
      }
      return FlowUnit.Default;
    });

  /// <inheritdoc/>
  public FlowIO<bool> Exists() =>
    FlowIO.Lift(() =>
    {
      lock (_lock)
      {
        return _hasData;
      }
    });

  /// <inheritdoc/>
  public FlowIO<ValidationResult> InspectShallow(int sampleSize) =>
    FlowIO.Lift(() =>
    {
      lock (_lock)
      {
        return _hasData
          ? ValidationResult.Success()
          : ValidationResult.Failure(
              catalogKey: typeof(T).Name,
              errorType: ValidationErrorType.NotFound,
              message: $"In-memory adapter for '{typeof(T).Name}' has no data — Save() must be called before inspection succeeds.",
              details: "Memory adapters are not pre-populated; a fresh instance inspects as missing until written."
            );
      }
    });

  /// <inheritdoc/>
  public FlowIO<ValidationResult> InspectDeep() => InspectShallow(sampleSize: 0);

  /// <inheritdoc/>
  public FlowIO<ValidationResult> InspectTarget() =>
    FlowIO.Pure(ValidationResult.Success());
}
