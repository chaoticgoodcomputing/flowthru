namespace Flowthru.Spaceflights.Data.Schemas.Models;

/// <summary>
/// Multi-output schema for train/test split operation.
/// Pure data schema with no catalog coupling.
/// 
/// Properties will be mapped to catalog entries at pipeline registration time
/// using OutputMapping&lt;T&gt; to maintain separation of concerns:
/// - Schema layer: Pure data shape definitions
/// - Catalog layer: Data storage/naming bindings
/// </summary>
public record SplitDataOutputs
{
  /// <summary>
  /// Training features
  /// </summary>
  public required IEnumerable<FeatureRow> XTrain { get; init; }

  /// <summary>
  /// Testing features
  /// </summary>
  public required IEnumerable<FeatureRow> XTest { get; init; }

  /// <summary>
  /// Training targets (prices)
  /// </summary>
  public required IEnumerable<decimal> YTrain { get; init; }

  /// <summary>
  /// Testing targets (prices)
  /// </summary>
  public required IEnumerable<decimal> YTest { get; init; }
}
