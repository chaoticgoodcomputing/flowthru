using Flowthru.Data.Schema;

namespace KedroIrisPython.Data._08_Reporting.Schemas;

/// <summary>
/// Schema for model accuracy metrics.
/// </summary>
[FlowthruSchema]
public partial record AccuracyReportSchema
{
  /// <summary>
  /// Overall model accuracy (0.0 to 1.0).
  /// </summary>
  [SerializedLabel("accuracy")]
  public double Accuracy { get; init; }

  /// <summary>
  /// Number of correct predictions.
  /// </summary>
  [SerializedLabel("correct_predictions")]
  public int CorrectPredictions { get; init; }

  /// <summary>
  /// Total number of test samples.
  /// </summary>
  [SerializedLabel("total_samples")]
  public int TotalSamples { get; init; }
}
