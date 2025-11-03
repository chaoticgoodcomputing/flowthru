using Flowthru.Abstractions;

namespace KedroSpaceflights.Pure.Data._04_Models.Schemas;

/// <summary>
/// Represents evaluation metrics for a regression model.
/// </summary>
public record ModelMetrics : IStructuredSerializable
{
  /// <summary>
  /// R² (coefficient of determination) score. 1.0 indicates perfect prediction, 0.0 indicates prediction no better than the mean.
  /// </summary>
  public decimal R2Score { get; init; }

  /// <summary>
  /// Mean Absolute Error (MAE) - average absolute difference between actual and predicted values.
  /// </summary>
  public decimal MeanAbsoluteError { get; init; }

  /// <summary>
  /// Maximum absolute error across all predictions.
  /// </summary>
  public decimal MaxError { get; init; }
}
