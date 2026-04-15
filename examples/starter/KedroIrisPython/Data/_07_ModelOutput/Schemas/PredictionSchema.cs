using Flowthru.Core.Abstractions;

namespace KedroIrisPython.Data._07_ModelOutput.Schemas;

/// <summary>
/// Schema for model predictions - predicted class indices.
/// </summary>
[FlowthruSchema]
public partial record PredictionSchema
{
    /// <summary>
    /// Predicted class index (0 = setosa, 1 = versicolor, 2 = virginica).
    /// </summary>
    [SerializedLabel("prediction")]
    public int Prediction { get; init; }
}
