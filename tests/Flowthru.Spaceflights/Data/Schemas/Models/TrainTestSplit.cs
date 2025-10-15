namespace Flowthru.Spaceflights.Data.Schemas.Models;

/// <summary>
/// Train/test split output containing all four split datasets.
/// Output of SplitDataNode (Option A: Composite type).
/// </summary>
public record TrainTestSplit
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
