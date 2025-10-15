namespace Flowthru.Spaceflights.Data.Schemas.Raw;

/// <summary>
/// Raw shuttle data as read from Excel file.
/// Matches structure of Datasets/01_Raw/shuttles.xlsx
/// </summary>
public record ShuttleRawSchema
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
  /// Shuttle type/model
  /// </summary>
  public string? ShuttleType { get; init; }

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
  /// Price as currency string (e.g., "$1,234,567")
  /// </summary>
  public required string Price { get; init; }

  /// <summary>
  /// D-check completion status as "t" or "f"
  /// </summary>
  public required string DCheckComplete { get; init; }

  /// <summary>
  /// Moon clearance completion status as "t" or "f"
  /// </summary>
  public required string MoonClearanceComplete { get; init; }
}
