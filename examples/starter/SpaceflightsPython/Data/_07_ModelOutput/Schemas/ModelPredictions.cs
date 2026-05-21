using Flowthru.Data.Schema;

namespace SpaceflightsPython.Data._07_ModelOutput.Schemas;

/// <summary>
/// Model prediction results containing actual and predicted values.
/// Used for generating confusion matrices and prediction accuracy visualizations.
/// </summary>
[FlowthruSchema]
public partial record ModelPredictions
{
  /// <summary>
  /// Actual value from test set.
  /// </summary>
  public double Actual { get; init; }

  /// <summary>
  /// Predicted value from the model.
  /// </summary>
  public double Predicted { get; init; }
}
