using Flowthru.Core.Abstractions;

namespace SpaceflightsPythonEFCore.Data._07_ModelOutput.Schemas;

/// <summary>
/// Evaluation metrics for the trained regression model.
/// Produced by the Python evaluate_model node.
/// </summary>
[FlowthruSchema]
public partial record ModelMetrics
{
  public required double R2Score { get; init; }
  public required double MeanAbsoluteError { get; init; }
  public required double MaxError { get; init; }
}
