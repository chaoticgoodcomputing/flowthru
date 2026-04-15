using Flowthru.Core.Abstractions;

namespace KedroIris.Data._05_ModelInput.Schemas;

/// <summary>
/// Represents feature vectors (X) for model training and testing.
/// Contains the four measurement features used as model inputs.
/// </summary>
[FlowthruSchema]
public partial record FeatureVectorSchema
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
}
