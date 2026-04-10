using Flowthru.Core.Abstractions;

namespace UmapReferenceComparisons.Data._01_Raw.Schemas;

/// <summary>
/// Input row for the Iris dataset.
/// </summary>
/// <remarks>
/// The Iris dataset contains 4 features for 150 flower samples across 3 species.
/// Features: sepal length, sepal width, petal length, petal width (all in cm).
/// </remarks>
public record IrisInputRow
  : IFlatSchema,
    IBinarySerializable,
    IStructuredSerializable,
    ITextSerializable
{
  /// <summary>
  /// Unique observation identifier (GUID).
  /// </summary>
  [SerializedLabel("id")]
  public string Id { get; init; } = null!;

  /// <summary>
  /// Sepal length in centimeters.
  /// </summary>
  [SerializedLabel("sepal_length")]
  public float SepalLength { get; init; }

  /// <summary>
  /// Sepal width in centimeters.
  /// </summary>
  [SerializedLabel("sepal_width")]
  public float SepalWidth { get; init; }

  /// <summary>
  /// Petal length in centimeters.
  /// </summary>
  [SerializedLabel("petal_length")]
  public float PetalLength { get; init; }

  /// <summary>
  /// Petal width in centimeters.
  /// </summary>
  [SerializedLabel("petal_width")]
  public float PetalWidth { get; init; }

  /// <summary>
  /// Class label (0=setosa, 1=versicolor, 2=virginica).
  /// </summary>
  [SerializedLabel("label")]
  public int Label { get; init; }
}
