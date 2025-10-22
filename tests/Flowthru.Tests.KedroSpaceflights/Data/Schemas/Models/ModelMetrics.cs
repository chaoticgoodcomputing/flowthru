namespace Flowthru.Tests.KedroSpaceflights.Data.Schemas.Models;

/// <summary>
/// Model evaluation metrics.
/// Output of EvaluateModelNode.
/// </summary>
public record ModelMetrics
{
  /// <summary>
  /// R² Score (coefficient of determination)
  /// </summary>
  public double R2Score { get; init; }

  /// <summary>
  /// Mean Absolute Error
  /// </summary>
  public double MeanAbsoluteError { get; init; }

  /// <summary>
  /// Maximum Error
  /// </summary>
  public double MaxError { get; init; }

  /// <summary>
  /// Root Mean Squared Error
  /// </summary>
  public double RootMeanSquaredError { get; init; }
}
