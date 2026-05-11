using Flowthru.Data.Schema;

namespace KedroSpaceflightsCustom.Data._05_ModelOutput.Schemas;

/// <summary>
/// Model evaluation metrics.
/// Output of EvaluateModelStep.
/// </summary>
public record ModelMetrics
  : IFlatSchema,
    ITextSerializable,
    IBinarySerializable,
    IStructuredSerializable
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
