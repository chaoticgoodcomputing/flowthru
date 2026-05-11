using Flowthru.Data.Schema;

namespace SpaceflightsStagingSchema.Data._07_ModelOutput.Schemas;

[FlowthruSchema]
public partial record ModelMetrics
{
  public required decimal R2Score { get; init; }
  public required decimal MeanAbsoluteError { get; init; }
  public required decimal MaxError { get; init; }
}
