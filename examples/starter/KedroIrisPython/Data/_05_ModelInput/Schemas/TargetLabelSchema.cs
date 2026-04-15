using Flowthru.Core.Abstractions;

namespace KedroIrisPython.Data._05_ModelInput.Schemas;

/// <summary>
/// Target label schema with one-hot encoded species classes.
/// Each species (setosa, versicolor, virginica) has its own column.
/// </summary>
[FlowthruSchema]
public partial record TargetLabelSchema
{
    /// <summary>
    /// One-hot encoded flag for Iris-setosa (1.0 if true, 0.0 if false).
    /// </summary>
    [SerializedLabel("Iris-setosa")]
    public double IrisSetosa { get; init; }

    /// <summary>
    /// One-hot encoded flag for Iris-versicolor (1.0 if true, 0.0 if false).
    /// </summary>
    [SerializedLabel("Iris-versicolor")]
    public double IrisVersicolor { get; init; }

    /// <summary>
    /// One-hot encoded flag for Iris-virginica (1.0 if true, 0.0 if false).
    /// </summary>
    [SerializedLabel("Iris-virginica")]
    public double IrisVirginica { get; init; }
}
