using Flowthru.Abstractions;

namespace KedroIris.Data._07_ModelOutput.Schemas;

/// <summary>
/// Represents a single prediction from the classification model.
/// </summary>
[FlowthruSchema]
public partial record PredictionSchema
{
  /// <summary>
  /// Predicted class index (0=setosa, 1=versicolor, 2=virginica).
  /// </summary>
  [SerializedLabel("predicted_class")]
  public required int PredictedClass { get; init; }
}
