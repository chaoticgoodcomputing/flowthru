using System.ComponentModel.DataAnnotations;

namespace Flowthru.Spaceflights.Data.Schemas.Processed;

/// <summary>
/// Processed shuttle data with type conversions applied.
/// Output of PreprocessShuttlesNode.
/// </summary>
public record ShuttleSchema
{
  /// <summary>
  /// Shuttle identifier
  /// </summary>
  [Required]
  public string Id { get; init; } = null!;

  /// <summary>
  /// Company identifier (foreign key to companies)
  /// </summary>
  [Required]
  public string CompanyId { get; init; } = null!;

  /// <summary>
  /// Shuttle type/model
  /// </summary>
  public string? ShuttleType { get; init; }

  /// <summary>
  /// Number of engines
  /// </summary>
  public int? Engines { get; init; }

  /// <summary>
  /// Passenger capacity
  /// </summary>
  public int? PassengerCapacity { get; init; }

  /// <summary>
  /// Crew size
  /// </summary>
  public int? Crew { get; init; }

  /// <summary>
  /// Price in dollars
  /// </summary>
  public decimal Price { get; init; }

  /// <summary>
  /// D-check completion status
  /// </summary>
  public bool DCheckComplete { get; init; }

  /// <summary>
  /// Moon clearance completion status
  /// </summary>
  public bool MoonClearanceComplete { get; init; }
}
