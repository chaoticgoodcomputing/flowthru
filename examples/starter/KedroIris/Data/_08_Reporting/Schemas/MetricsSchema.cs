using Flowthru.Abstractions;

namespace KedroIris.Data._08_Reporting.Schemas;

/// <summary>
/// Represents model evaluation metrics for the iris classification task.
/// </summary>
[FlowthruSchema]
public partial record MetricsSchema
{
  /// <summary>
  /// Model accuracy on the test set (proportion of correct predictions).
  /// </summary>
  [SerializedLabel("accuracy")]
  public required double Accuracy { get; init; }

  /// <summary>
  /// Number of correct predictions.
  /// </summary>
  [SerializedLabel("num_correct")]
  public required int NumCorrect { get; init; }

  /// <summary>
  /// Total number of test samples.
  /// </summary>
  [SerializedLabel("num_total")]
  public required int NumTotal { get; init; }
}
