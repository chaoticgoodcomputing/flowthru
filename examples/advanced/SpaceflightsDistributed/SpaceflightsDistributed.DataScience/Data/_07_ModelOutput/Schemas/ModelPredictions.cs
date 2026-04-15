using Flowthru.Core.Abstractions;

namespace SpaceflightsDistributed.DataScience.Data._07_ModelOutput.Schemas;

[FlowthruSchema]
public partial record ModelPredictions
{
  public double Actual { get; init; }
  public double Predicted { get; init; }
}
