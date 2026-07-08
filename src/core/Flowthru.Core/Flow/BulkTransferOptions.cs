namespace Flowthru.Flow;

/// <summary>
/// Options for <see cref="FlowBuilderBulkTransferExtensions.AddBulkTransfer{T}"/>.
/// The defaults prefer the fastest available rung and allow a visible
/// downgrade to the streaming fallback when no native pairing exists.
/// </summary>
public sealed record BulkTransferOptions
{
  /// <summary>Default options: native preferred, streaming fallback allowed.</summary>
  public static BulkTransferOptions Default { get; } = new();

  /// <summary>
  /// When <c>true</c>, an unavailable native path is a pre-flight error
  /// instead of a downgrade to the streaming fallback — for flows that
  /// would rather fail than move a large table row-at-a-time. Note that
  /// the native rung's execution machinery has not shipped yet, so in the
  /// current version this option fails pre-flight for every pairing.
  /// </summary>
  public bool RequireNative { get; init; }
}
