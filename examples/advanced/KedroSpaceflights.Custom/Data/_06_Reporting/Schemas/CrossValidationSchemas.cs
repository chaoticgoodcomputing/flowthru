using Flowthru.Abstractions;

namespace KedroSpaceflights.Custom.Data._06_Reporting.Schemas;

/// <summary>
/// Results from cross-validation analysis
///
/// Acts as a SerializedLabel test for JSON serialization.
/// </summary>
[FlowthruSchema]
public partial record CrossValidationResults
{
  /// <summary>
  /// Metrics for each fold
  /// </summary>
  [SerializedLabel("fold_metrics")]
  public List<FoldMetric> FoldMetrics { get; init; } = new();

  /// <summary>
  /// Mean R² across all folds
  /// </summary>
  [SerializedLabel("mean_r2_score")]
  public double MeanR2Score { get; init; }

  /// <summary>
  /// Standard deviation of R² across folds
  /// </summary>
  [SerializedLabel("std_dev_r2_score")]
  public double StdDevR2Score { get; init; }

  /// <summary>
  /// Minimum R² across folds
  /// </summary>
  [SerializedLabel("min_r2_score")]
  public double MinR2Score { get; init; }

  /// <summary>
  /// Maximum R² across folds
  /// </summary>
  [SerializedLabel("max_r2_score")]
  public double MaxR2Score { get; init; }

  /// <summary>
  /// Number of folds used
  /// </summary>
  [SerializedLabel("num_folds")]
  public int NumFolds { get; init; }

  /// <summary>
  /// Kedro's reference R² score
  /// </summary>
  [SerializedLabel("kedro_r2_score")]
  public double KedroR2Score { get; init; }

  /// <summary>
  /// Absolute difference from Kedro score
  /// </summary>
  [SerializedLabel("difference_from_kedro")]
  public double DifferenceFromKedro { get; init; }
}

/// <summary>
/// Metrics for a single cross-validation fold
/// </summary>
[FlowthruSchema]
public partial record FoldMetric
{
  /// <summary>
  /// Fold number (1-indexed)
  /// </summary>
  [SerializedLabel("fold_number")]
  public int FoldNumber { get; init; }

  /// <summary>
  /// R² score for this fold
  /// </summary>
  [SerializedLabel("r2_score")]
  public double R2Score { get; init; }

  /// <summary>
  /// Mean absolute error for this fold
  /// </summary>
  [SerializedLabel("mean_absolute_error")]
  public double MeanAbsoluteError { get; init; }

  /// <summary>
  /// Root mean squared error for this fold
  /// </summary>
  [SerializedLabel("root_mean_squared_error")]
  public double RootMeanSquaredError { get; init; }

  /// <summary>
  /// Loss function value for this fold
  /// </summary>
  [SerializedLabel("loss_function_value")]
  public double LossFunctionValue { get; init; }
}
