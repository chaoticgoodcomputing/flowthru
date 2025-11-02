using Flowthru.Abstractions;

namespace Flowthru.Tests.KedroSpaceflights.Data.Schemas.Models;

/// <summary>
/// Results from cross-validation analysis
/// </summary>
public record ModelPredictions : IFlatSerializable
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
