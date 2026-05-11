using Flowthru.Data.Schema;

namespace KedroSpaceflightsCustom.Data._02_Intermediate.Schemas;

/// <summary>
/// Processed shuttle data with type conversions applied.
/// Output of PreprocessShuttlesStep.
/// </summary>
public record ShuttleSchema
  : IFlatSchema,
    ITextSerializable,
    IBinarySerializable,
    IStructuredSerializable
{
  /// <summary>
  /// Shuttle identifier
  /// </summary>
  public required string Id { get; init; }

  /// <summary>
  /// Company identifier (foreign key to companies)
  /// </summary>
  public required string CompanyId { get; init; }

  /// <summary>
  /// Shuttle location/origin
  /// </summary>
  public required string ShuttleLocation { get; init; }

  /// <summary>
  /// Shuttle type/model
  /// </summary>
  public required string ShuttleType { get; init; }

  /// <summary>
  /// Engine type (e.g., Plasma, Quantum)
  /// </summary>
  public required string EngineType { get; init; }

  /// <summary>
  /// Engine vendor/manufacturer
  /// </summary>
  public required string EngineVendor { get; init; }

  /// <summary>
  /// Number of engines
  /// </summary>
  public required int Engines { get; init; }

  /// <summary>
  /// Passenger capacity
  /// </summary>
  public required int PassengerCapacity { get; init; }

  /// <summary>
  /// Crew size
  /// </summary>
  public required int Crew { get; init; }

  /// <summary>
  /// Cancellation policy (e.g., moderate, strict, flexible)
  /// </summary>
  public required string CancellationPolicy { get; init; }

  /// <summary>
  /// Price in dollars
  /// </summary>
  public required decimal Price { get; init; }

  /// <summary>
  /// D-check completion status
  /// </summary>
  public required bool DCheckComplete { get; init; }

  /// <summary>
  /// Moon clearance completion status
  /// </summary>
  public required bool MoonClearanceComplete { get; init; }
}
