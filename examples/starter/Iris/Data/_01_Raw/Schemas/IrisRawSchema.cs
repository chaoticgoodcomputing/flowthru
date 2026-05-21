using Flowthru.Data.Schema;

namespace Iris.Data._01_Raw.Schemas;

/// <summary>
/// Represents raw iris data as imported from CSV files.
/// Contains sepal and petal measurements along with species classification.
/// </summary>
[FlowthruSchema]
public partial record IrisRawSchema
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
  /// Species classification (setosa, versicolor, or virginica).
  /// </summary>
  [SerializedLabel("species")]
  public required string Species { get; init; }
}
