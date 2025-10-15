namespace Flowthru.Spaceflights.Data.Schemas.Processed;

/// <summary>
/// Model input table combining shuttles, companies, and reviews.
/// Output of CreateModelInputTableNode.
/// </summary>
public record ModelInputSchema
{
  /// <summary>
  /// Shuttle identifier
  /// </summary>
  public required string ShuttleId { get; init; }

  /// <summary>
  /// Company identifier
  /// </summary>
  public required string CompanyId { get; init; }

  /// <summary>
  /// Company name/location
  /// </summary>
  public string? CompanyLocation { get; init; }

  /// <summary>
  /// Shuttle type
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
  /// D-check completion status
  /// </summary>
  public bool DCheckComplete { get; init; }

  /// <summary>
  /// Moon clearance completion status
  /// </summary>
  public bool MoonClearanceComplete { get; init; }

  /// <summary>
  /// IATA approval status
  /// </summary>
  public bool IataApproved { get; init; }

  /// <summary>
  /// Company rating (0.0 to 1.0)
  /// </summary>
  public decimal CompanyRating { get; init; }

  /// <summary>
  /// Review scores rating
  /// </summary>
  public decimal? ReviewScoresRating { get; init; }

  /// <summary>
  /// Shuttle price (target variable for ML)
  /// </summary>
  public decimal Price { get; init; }
}
