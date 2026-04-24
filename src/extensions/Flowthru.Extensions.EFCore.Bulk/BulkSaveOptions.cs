namespace Flowthru.Extensions.EFCore.Bulk;

/// <summary>
/// Configuration options for bulk save operations. Exposes the subset of
/// <c>EFCore.BulkExtensions.BulkConfig</c> properties that are relevant to
/// Flowthru catalog item save strategies.
/// </summary>
/// <remarks>
/// Use <c>with { }</c> syntax to customize from defaults:
/// <code>
/// new BulkSaveOptions { BatchSize = 5000, TimeoutSeconds = 120 }
/// </code>
/// </remarks>
public record BulkSaveOptions
{
  /// <summary>Number of rows per bulk operation batch. Default: 2000.</summary>
  public int BatchSize { get; init; } = 2000;

  /// <summary>
  /// Timeout in seconds for the bulk copy operation. <c>null</c> uses the
  /// provider default (typically 30 seconds). Set to <c>0</c> for no limit.
  /// </summary>
  public int? TimeoutSeconds { get; init; }

  /// <summary>Preserve the insert order of entities. Default: true.</summary>
  public bool PreserveInsertOrder { get; init; } = true;

  /// <summary>
  /// Reload database-generated identity values back into entities after insert.
  /// Required when downstream steps depend on auto-generated PKs. Default: false.
  /// </summary>
  public bool SetOutputIdentity { get; init; }

  /// <summary>
  /// PostgreSQL-specific: use UNLOGGED temp tables for merge operations.
  /// Faster but not crash-safe. Default: false.
  /// </summary>
  public bool UseUnlogged { get; init; }

  /// <summary>
  /// Whitelist of CLR property names to include in the bulk operation.
  /// <c>null</c> includes all mapped properties.
  /// Mutually exclusive with <see cref="PropertiesToExclude"/>.
  /// </summary>
  public IReadOnlyList<string>? PropertiesToInclude { get; init; }

  /// <summary>
  /// Blacklist of CLR property names to exclude from the bulk operation.
  /// <c>null</c> excludes nothing.
  /// Mutually exclusive with <see cref="PropertiesToInclude"/>.
  /// </summary>
  public IReadOnlyList<string>? PropertiesToExclude { get; init; }

  /// <summary>
  /// Optional progress callback invoked with a percentage (0–100) during the
  /// bulk operation. Useful for logging large loads.
  /// </summary>
  public Action<decimal>? OnProgress { get; init; }
}
