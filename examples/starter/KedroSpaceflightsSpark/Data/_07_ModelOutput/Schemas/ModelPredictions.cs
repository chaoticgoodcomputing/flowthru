using Flowthru.Core.Abstractions;

namespace KedroSpaceflightsSpark.Data._07_ModelOutput.Schemas;

[FlowthruSchema]
public partial record ModelPredictions
{
  public double Actual { get; init; }
  public double Predicted { get; init; }
}
