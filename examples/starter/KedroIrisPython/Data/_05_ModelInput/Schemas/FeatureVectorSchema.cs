using Flowthru.Core.Abstractions;

namespace KedroIrisPython.Data._05_ModelInput.Schemas;

/// <summary>
/// Feature vector schema for training and test datasets.
/// Contains the four iris measurements (sepal/petal dimensions).
/// </summary>
[FlowthruSchema]
public partial record FeatureVectorSchema
{
    /// <summary>
    /// Sepal length in centimeters.
    /// </summary>
    [SerializedLabel("sepal_length")]
    public double SepalLength { get; init; }

    /// <summary>
    /// Sepal width in centimeters.
    /// </summary>
    [SerializedLabel("sepal_width")]
    public double SepalWidth { get; init; }

    /// <summary>
    /// Petal length in centimeters.
    /// </summary>
    [SerializedLabel("petal_length")]
    public double PetalLength { get; init; }

    /// <summary>
    /// Petal width in centimeters.
    /// </summary>
    [SerializedLabel("petal_width")]
    public double PetalWidth { get; init; }
}
