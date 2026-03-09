using Flowthru.Abstractions;

namespace KedroIrisPython.Data._01_Raw.Schemas;

/// <summary>
/// Represents raw iris flower measurement data.
/// Contains four measurements (sepal/petal dimensions) and species label.
/// </summary>
[FlowthruSchema]
public partial record IrisRawSchema
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

  /// <summary>
  /// Species classification (setosa, versicolor, or virginica).
  /// </summary>
  [SerializedLabel("species")]
  public string Species { get; init; } = null!;
}
