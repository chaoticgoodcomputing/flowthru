using Flowthru.Data.Schema;

namespace SpaceflightsEnhanced.Data._05_ModelOutput.Schemas;

/// <summary>
/// Results from cross-validation analysis
/// </summary>
public record ModelPredictions
  : IFlatSchema,
    ITextSerializable,
    IBinarySerializable,
    IStructuredSerializable
{
  /// <summary>
  /// Actual value from test set
  /// </summary>
  public double Actual { get; init; }

  /// <summary>
  /// Predicted value from the model
  /// </summary>
  public double Predicted { get; init; }
}
