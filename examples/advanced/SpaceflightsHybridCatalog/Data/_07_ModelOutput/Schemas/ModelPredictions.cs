using Flowthru.Data.Schema;

namespace SpaceflightsHybridCatalog.Data._07_ModelOutput.Schemas;

/// <summary>
/// Model prediction results containing actual and predicted values, used for
/// generating confusion matrices and prediction accuracy visualizations.
/// </summary>
[FlowthruSchema]
public partial record ModelPredictions
{
  public double Actual { get; init; }
  public double Predicted { get; init; }
}
