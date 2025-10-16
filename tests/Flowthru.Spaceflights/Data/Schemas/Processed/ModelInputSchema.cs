using System.ComponentModel.DataAnnotations;

namespace Flowthru.Spaceflights.Data.Schemas.Processed;

/// <summary>
/// Model input table combining shuttles, companies, and reviews.
/// Output of CreateModelInputTableNode.
/// Matches Kedro's full merged dataset with all 27 columns for apples-to-apples comparison.
/// </summary>
public record ModelInputSchema
{
  // Shuttle columns (from shuttles table)
  
  /// <summary>
  /// Shuttle location/origin
  /// </summary>
  public string? ShuttleLocation { get; init; }

  /// <summary>
  /// Shuttle type
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
  public int? Engines { get; init; }

  /// <summary>
  /// Passenger capacity
  /// </summary>
  public int? PassengerCapacity { get; init; }

  /// <summary>
  /// Cancellation policy
  /// </summary>
  public string? CancellationPolicy { get; init; }

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
  /// Shuttle price (target variable for ML)
  /// </summary>
  public decimal Price { get; init; }

  /// <summary>
  /// Company identifier (from shuttle.company_id)
  /// </summary>
  [Required]
  public string CompanyId { get; init; } = null!;

  /// <summary>
  /// Shuttle identifier (from review.shuttle_id)
  /// </summary>
  [Required]
  public string ShuttleId { get; init; } = null!;

  // Review columns (from reviews table)

  /// <summary>
  /// Overall review score rating
  /// </summary>
  public decimal? ReviewScoresRating { get; init; }

  /// <summary>
  /// Review comfort score
  /// </summary>
  public decimal? ReviewScoresComfort { get; init; }

  /// <summary>
  /// Review amenities score
  /// </summary>
  public decimal? ReviewScoresAmenities { get; init; }

  /// <summary>
  /// Review trip score
  /// </summary>
  public decimal? ReviewScoresTrip { get; init; }

  /// <summary>
  /// Review crew score
  /// </summary>
  public decimal? ReviewScoresCrew { get; init; }

  /// <summary>
  /// Review location score
  /// </summary>
  public decimal? ReviewScoresLocation { get; init; }

  /// <summary>
  /// Review price score
  /// </summary>
  public decimal? ReviewScoresPrice { get; init; }

  /// <summary>
  /// Number of reviews
  /// </summary>
  public int? NumberOfReviews { get; init; }

  /// <summary>
  /// Reviews per month
  /// </summary>
  public decimal? ReviewsPerMonth { get; init; }

  // Company columns (from companies table)

  /// <summary>
  /// Company identifier (from company.id, duplicate of CompanyId above)
  /// </summary>
  public string? Id { get; init; }

  /// <summary>
  /// Company rating (0.0 to 1.0)
  /// </summary>
  public decimal? CompanyRating { get; init; }

  /// <summary>
  /// Company location/country
  /// </summary>
  public string? CompanyLocation { get; init; }

  /// <summary>
  /// Total fleet count
  /// </summary>
  public decimal? TotalFleetCount { get; init; }

  /// <summary>
  /// IATA approval status
  /// </summary>
  public bool IataApproved { get; init; }
}
