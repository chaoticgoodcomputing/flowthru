namespace Flowthru.Tests.KedroSpaceflights.Data.Schemas.Models;

/// <summary>
/// Results from cross-validation analysis
/// </summary>
public record CrossValidationResults {
  /// <summary>
  /// Metrics for each fold
  /// </summary>
  public List<FoldMetric> FoldMetrics { get; init; } = new();

  /// <summary>
  /// Mean R² across all folds
  /// </summary>
  public double MeanR2Score { get; init; }

  /// <summary>
  /// Standard deviation of R² across folds
  /// </summary>
  public double StdDevR2Score { get; init; }

  /// <summary>
  /// Minimum R² across folds
  /// </summary>
  public double MinR2Score { get; init; }

  /// <summary>
  /// Maximum R² across folds
  /// </summary>
  public double MaxR2Score { get; init; }

  /// <summary>
  /// Number of folds used
  /// </summary>
  public int NumFolds { get; init; }

  /// <summary>
  /// Kedro's reference R² score
  /// </summary>
  public double KedroR2Score { get; init; }

  /// <summary>
  /// Absolute difference from Kedro score
  /// </summary>
  public double DifferenceFromKedro { get; init; }
}

/// <summary>
/// Metrics for a single cross-validation fold
/// </summary>
public record FoldMetric {
  /// <summary>
  /// Fold number (1-indexed)
  /// </summary>
  public int FoldNumber { get; init; }

  /// <summary>
  /// R² score for this fold
  /// </summary>
  public double R2Score { get; init; }

  /// <summary>
  /// Mean absolute error for this fold
  /// </summary>
  public double MeanAbsoluteError { get; init; }

  /// <summary>
  /// Root mean squared error for this fold
  /// </summary>
  public double RootMeanSquaredError { get; init; }

  /// <summary>
  /// Loss function value for this fold
  /// </summary>
  public double LossFunctionValue { get; init; }
}

/// <summary>
/// Parameters for cross-validation analysis
/// </summary>
public record CrossValidationOptions {
  /// <summary>
  /// Number of folds for k-fold cross-validation
  /// </summary>
  public int NumFolds { get; init; }

  /// <summary>
  /// Base random seed for reproducibility
  /// </summary>
  public int BaseSeed { get; init; }

  /// <summary>
  /// Kedro's reference R² score for comparison.
  /// 
  /// As an additional clarification, this metric is NOT meant to determine if our pipeline "beats"
  /// Kedro's implementation. Rather, it serves as a benchmark to ensure our implementation is
  /// comfortably close to Kedro's original spaceflights example, indicating that our data processing
  /// and modeling steps are correctly aligned.
  /// </summary>
  public float KedroReferenceR2Score { get; init; }
}
