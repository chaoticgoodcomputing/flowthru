using Flowthru.Data.Capabilities;
using Flowthru.Effects;

namespace Flowthru.Data.Storage;

/// <summary>
/// Null storage adapter for side-effect-only nodes that produce no meaningful data.
/// </summary>
/// <typeparam name="T">The data type (typically NoData)</typeparam>
/// <remarks>
/// <para>
/// <strong>Use Case:</strong> Nodes that perform side effects (logging, visualization, alerts)
/// but don't produce data that downstream nodes need.
/// </para>
/// <para>
/// <strong>Capabilities:</strong>
/// </para>
/// <list type="bullet">
/// <item>ISeedable: false (null entries cannot be Layer 0 inputs)</item>
/// <item>IReadOnly: true (all operations are no-ops)</item>
/// </list>
/// </remarks>
public sealed class NullStorageAdapter<T> : IStorageAdapter<T>, IReadOnly
{
  /// <inheritdoc/>
  public bool IsReadOnly => true;

  /// <inheritdoc/>
  public FlowIO<T> Load() =>
    FlowIO.LiftAsync<T>(async () =>
    {
      throw new NotSupportedException("NullStorageAdapter does not support Load operations");
    });

  /// <inheritdoc/>
  public FlowIO<FlowUnit> Save(T data) =>
    FlowIO.LiftAsync(async () =>
    {
      // No-op: side-effect-only nodes don't save data
      return FlowUnit.Default;
    });

  /// <inheritdoc/>
  public FlowIO<bool> Exists() =>
    FlowIO.LiftAsync(async () =>
    {
      return false; // Null entries never exist as seedable data
    });
}
