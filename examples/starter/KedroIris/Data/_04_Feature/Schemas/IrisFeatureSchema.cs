using Flowthru.Data.Schema;

namespace KedroIris.Data._04_Feature.Schemas;

/// <summary>
/// Represents iris data with one-hot encoded species classifications.
/// Features include the original measurements plus binary indicators for each species.
/// </summary>
[FlowthruSchema]
public partial record IrisFeatureSchema
{
  /// <summary>
  /// Sepal length in centimeters.
  /// </summary>
  [SerializedLabel("sepal_length")]
  public required double SepalLength { get; init; }

  /// <summary>
  /// Sepal width in centimeters.
  /// </summary>
  [SerializedLabel("sepal_width")]
  public required double SepalWidth { get; init; }

  /// <summary>
  /// Petal length in centimeters.
  /// </summary>
  [SerializedLabel("petal_length")]
  public required double PetalLength { get; init; }

  /// <summary>
  /// Petal width in centimeters.
  /// </summary>
  [SerializedLabel("petal_width")]
  public required double PetalWidth { get; init; }

  /// <summary>
  /// Binary indicator for setosa species (1.0 if setosa, 0.0 otherwise).
  /// </summary>
  [SerializedLabel("setosa")]
  public required double Setosa { get; init; }

  /// <summary>
  /// Binary indicator for versicolor species (1.0 if versicolor, 0.0 otherwise).
  /// </summary>
  [SerializedLabel("versicolor")]
  public required double Versicolor { get; init; }

  /// <summary>
  /// Binary indicator for virginica species (1.0 if virginica, 0.0 otherwise).
  /// </summary>
  [SerializedLabel("virginica")]
  public required double Virginica { get; init; }
}
