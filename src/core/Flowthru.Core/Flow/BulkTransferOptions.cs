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
  /// would rather fail than move a large table row-at-a-time. A pairing
  /// whose endpoints declare matching bulk capabilities (same provider,
  /// same wire format — e.g. two Npgsql-backed Postgres tables) passes
  /// and runs natively; any other pairing fails pre-flight with the
  /// pairing verdict in the error.
  /// </summary>
  public bool RequireNative { get; init; }
}
