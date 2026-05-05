using Flowthru.Core.Abstractions;

namespace SpaceflightsStagingSchema.Data._07_ModelOutput.Schemas;

[FlowthruSchema]
public partial record ModelPredictions
{
  /// <summary>Auto-generated surrogate key.</summary>
  public int Id { get; init; }

  public double Actual { get; init; }
  public double Predicted { get; init; }
}
