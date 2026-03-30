namespace Flowthru.Data.Storage.Strategies;

/// <summary>
/// Options for configuring storage entry creation.
/// </summary>
/// <remarks>
/// Provides flexibility for strategy-specific configuration without
/// requiring changes to the factory interface.
/// </remarks>
public sealed record StorageOptions
{
  /// <summary>
  /// Relative path or identifier for the storage location.
  /// </summary>
  /// <remarks>
  /// Interpretation depends on the strategy:
  /// - CSV: File path relative to base directory (e.g., "_01_Raw/data.csv")
  /// - Database: Table name or qualified identifier (e.g., "dbo.Companies")
  /// - Memory: Ignored (memory storage has no path)
  /// </remarks>
  public string? Path { get; init; }

  /// <summary>
  /// Additional strategy-specific metadata.
  /// </summary>
  /// <remarks>
  /// Examples:
  /// - Excel: {"SheetName": "Data"}
  /// - Database: {"Schema": "analytics", "Timeout": 30}
  /// - Parquet: {"CompressionCodec": "SNAPPY"}
  /// </remarks>
  public Dictionary<string, object>? Metadata { get; init; }

  /// <summary>
  /// Creates storage options with a path.
  /// </summary>
  public static StorageOptions WithPath(string path) => new() { Path = path };
}
