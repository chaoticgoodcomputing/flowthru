using Flowthru.Data.Schema;

namespace KedroIrisFUnit.Data._05_ModelInput.Schemas;

/// <summary>
/// Represents target labels (Y) for model training and testing.
/// Contains one-hot encoded species classifications.
/// </summary>
[FlowthruSchema]
public partial record TargetLabelSchema
{
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
