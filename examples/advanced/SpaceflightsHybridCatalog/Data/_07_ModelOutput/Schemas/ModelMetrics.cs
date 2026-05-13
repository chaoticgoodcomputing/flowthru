using Flowthru.Data.Schema;

namespace SpaceflightsHybridCatalog.Data._07_ModelOutput.Schemas;

/// <summary>Evaluation metrics for a regression model.</summary>
[FlowthruSchema]
public partial record ModelMetrics
{
  public required decimal R2Score { get; init; }
  public required decimal MeanAbsoluteError { get; init; }
  public required decimal MaxError { get; init; }
}
