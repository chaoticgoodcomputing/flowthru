using Flowthru.Data.Schema;

namespace KedroSpaceflightsPython.Data._07_ModelOutput.Schemas;

/// <summary>
/// Represents evaluation metrics for a regression model.
/// </summary>
/// <remarks>
/// Uses required members to enforce that all critical metrics must be set
/// during construction, ensuring complete model evaluation reporting.
/// </remarks>
[FlowthruSchema]
public partial record ModelMetrics
{
  /// <summary>
  /// R² (coefficient of determination) score. 1.0 indicates perfect prediction, 0.0 indicates prediction no better than the mean.
  /// </summary>
  public required double R2Score { get; init; }

  /// <summary>
  /// Mean Absolute Error (MAE) - average absolute difference between actual and predicted values.
  /// </summary>
  public required double MeanAbsoluteError { get; init; }

  /// <summary>
  /// Maximum absolute error across all predictions.
  /// </summary>
  public required double MaxError { get; init; }
}
