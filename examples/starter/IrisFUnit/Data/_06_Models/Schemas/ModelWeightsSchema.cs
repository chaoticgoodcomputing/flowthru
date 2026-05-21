using Flowthru.Data.Schema;

namespace IrisFUnit.Data._06_Models.Schemas;

/// <summary>
/// Represents a trained multi-class logistic regression model.
/// The weights matrix contains coefficients for all three species classifiers.
/// </summary>
[FlowthruSchema]
public partial record ModelWeightsSchema
{
  /// <summary>
  /// Flattened weights matrix serialized as JSON array.
  /// Shape: (num_features + 1, num_classes) where +1 accounts for bias term.
  /// Each column represents the weights for one species classifier.
  /// </summary>
  [SerializedLabel("weights")]
  public required double[] Weights { get; init; }

  /// <summary>
  /// Number of features (excluding bias term).
  /// </summary>
  [SerializedLabel("num_features")]
  public required int NumFeatures { get; init; }

  /// <summary>
  /// Number of classes (species).
  /// </summary>
  [SerializedLabel("num_classes")]
  public required int NumClasses { get; init; }
}
