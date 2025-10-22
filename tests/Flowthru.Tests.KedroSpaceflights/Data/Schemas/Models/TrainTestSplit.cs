namespace Flowthru.Tests.KedroSpaceflights.Data.Schemas.Models;

/// <summary>
/// [DEPRECATED - Kept for reference]
/// 
/// Train/test split output containing all four split datasets as a composite type.
/// This was the original approach before adopting multi-output with OutputMapping&lt;T&gt;.
/// 
/// Current implementation uses SplitDataOutputs with OutputMapping to create
/// individual catalog entries (x_train, x_test, y_train, y_test) instead.
/// 
/// This composite pattern is still valid for cases where outputs are truly atomic
/// and should always be consumed together.
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
