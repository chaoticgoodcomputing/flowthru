using Flowthru.Core.Data.Capabilities;
using Flowthru.Core.Effects;

namespace Flowthru.Core.Data.Storage;

/// <summary>
/// Null storage adapter for side-effect-only nodes that produce no meaningful data.
/// </summary>
/// <typeparam name="T">The data type (typically NoData)</typeparam>
/// <remarks>
/// <para>
/// <strong>Use Case:</strong> Steps that perform side effects (logging, visualization, alerts)
/// but don't produce data that downstream nodes need.
/// </para>
/// <para>
/// <strong>Storage Traits:</strong>
/// </para>
/// <list type="bullet">
/// <item>CanWrite: false (Save is a no-op)</item>
/// <item>CanRead: false (Load throws NotSupportedException)</item>
/// </list>
/// </remarks>
public sealed class NullStorageAdapter<T> : IStorageAdapter<T>
{
  /// <inheritdoc/>
  public StorageTraits Traits => new StorageTraits { CanWrite = false, CanRead = false };

  /// <inheritdoc/>
  public FlowIO<T> Load() =>
    FlowIO.Lift<T>(() =>
    {
      throw new NotSupportedException("NullStorageAdapter does not support Load operations");
    });

  /// <inheritdoc/>
  public FlowIO<FlowUnit> Save(T data) =>
    FlowIO.Lift(() =>
    {
      // No-op: side-effect-only nodes don't save data
      return FlowUnit.Default;
    });

  /// <inheritdoc/>
  public FlowIO<bool> Exists() => FlowIO.Lift(() => false); // Null entries never exist as seedable data

  /// <inheritdoc/>
  public FlowIO<Data.Validation.ValidationResult> InspectShallow(int sampleSize) =>
    FlowIO.Pure(Data.Validation.ValidationResult.Success()); // No data required, inherently available

  /// <inheritdoc/>
  public FlowIO<Data.Validation.ValidationResult> InspectDeep() =>
    FlowIO.Pure(Data.Validation.ValidationResult.Success()); // No data required, inherently available

  /// <inheritdoc/>
  /// <remarks>Null adapters are write-only no-ops — no destination to validate.</remarks>
  public FlowIO<Data.Validation.ValidationResult> InspectTarget() =>
    FlowIO.Pure(Data.Validation.ValidationResult.Success());
}
