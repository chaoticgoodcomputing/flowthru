using System.ComponentModel.DataAnnotations;

namespace Flowthru.Tests.KedroSpaceflights.Data.Schemas.Raw;

/// <summary>
/// Raw shuttle data as read from Excel file.
/// Matches structure of Datasets/01_Raw/shuttles.xlsx
/// </summary>
public record ShuttleRawSchema {
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
  /// Shuttle location/origin
  /// </summary>
  public string? ShuttleLocation { get; init; }

  /// <summary>
  /// Shuttle type/model
  /// </summary>
  public string? ShuttleType { get; init; }

  /// <summary>
  /// Engine type (e.g., Plasma, Quantum)
  /// </summary>
  public string? EngineType { get; init; }

  /// <summary>
  /// Engine vendor/manufacturer
  /// </summary>
  public string? EngineVendor { get; init; }

  /// <summary>
  /// Number of engines
  /// </summary>
  public string? Engines { get; init; }

  /// <summary>
  /// Passenger capacity
  /// </summary>
  public string? PassengerCapacity { get; init; }

  /// <summary>
  /// Crew size
  /// </summary>
  public string? Crew { get; init; }

  /// <summary>
  /// Cancellation policy (e.g., moderate, strict, flexible)
  /// </summary>
  public string? CancellationPolicy { get; init; }

  /// <summary>
  /// Price as currency string (e.g., "$1,234,567")
  /// </summary>
  [Required]
  public string Price { get; init; } = null!;

  /// <summary>
  /// D-check completion status as "t" or "f"
  /// </summary>
  [Required]
  public string DCheckComplete { get; init; } = null!;

  /// <summary>
  /// Moon clearance completion status as "t" or "f"
  /// </summary>
  [Required]
  public string MoonClearanceComplete { get; init; } = null!;
}
