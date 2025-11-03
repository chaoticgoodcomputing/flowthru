using Flowthru.Abstractions;

namespace KedroSpaceflights.Custom.Data.Schemas.Models;

/// <summary>
/// Wrapper for target values (prices) used in model training and evaluation.
/// Required for Parquet serialization which doesn't support primitive collections directly.
/// </summary>
public record TargetValue
  : IFlatSchema,
    ITextSerializable,
    IBinarySerializable,
    IStructuredSerializable
{
  /// <summary>
  /// The target price value
  /// </summary>
  public decimal Price { get; init; }
}
