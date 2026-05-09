using Flowthru.Data.Schema;

namespace SpaceflightsDistributed.DataScience.Data._07_ModelOutput.Schemas;

[FlowthruSchema]
public partial record ModelPredictions
{
  public double Actual { get; init; }
  public double Predicted { get; init; }
}
