using Flowthru.Core.Abstractions;

namespace KedroSpaceflights.Custom.Data._05_ModelOutput.Schemas;

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
