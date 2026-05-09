using Flowthru.Data.Schema;

namespace SpaceflightsPythonEFCore.Data._07_ModelOutput.Schemas;

/// <summary>
/// Model prediction results containing actual and predicted values.
/// Produced by the Python generate_predictions node and stored in EFCore.
/// Consumed by the Python Reporting pipeline to generate visualizations.
/// </summary>
[FlowthruSchema]
public partial record ModelPredictions
{
  public double Actual { get; init; }
  public double Predicted { get; init; }
}
