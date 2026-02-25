using Flowthru.Abstractions;
using Flowthru.Data;

namespace KedroSpaceflights.Custom.Data._03_Primary.Schemas;

/// <summary>
/// Final model input data schema (result of joining companies, shuttles, and reviews).
/// Output of CreateModelInputNode.
/// </summary>
[FlowthruSchema]
public partial record ModelInputSchema
{
  // Shuttle columns (from shuttles table)

  /// <summary>
  /// Shuttle location/origin
  /// </summary>
  [SerializedLabel("shuttle_location")]
  public string? ShuttleLocation { get; init; }

  /// <summary>
  /// Shuttle type
  /// </summary>
  [SerializedLabel("shuttle_type")]
  public string? ShuttleType { get; init; }

  /// <summary>
  /// Engine type (e.g., Plasma, Quantum)
  /// </summary>
  [SerializedLabel("engine_type")]
  public string? EngineType { get; init; }

  /// <summary>
  /// Engine vendor/manufacturer
  /// </summary>
  [SerializedLabel("engine_vendor")]
  public string? EngineVendor { get; init; }

  /// <summary>
  /// Number of engines
  /// </summary>
  [SerializedLabel("engines")]
  public required int Engines { get; init; }

  /// <summary>
  /// Passenger capacity
  /// </summary>
  [SerializedLabel("passenger_capacity")]
  public required int PassengerCapacity { get; init; }

  /// <summary>
  /// Cancellation policy
  /// </summary>
  [SerializedLabel("cancellation_policy")]
  public string? CancellationPolicy { get; init; }

  /// <summary>
  /// Crew size
  /// </summary>
  [SerializedLabel("crew")]
  public required int Crew { get; init; }

  /// <summary>
  /// D-check completion status
  /// </summary>
  [SerializedLabel("d_check_complete")]
  public required bool DCheckComplete { get; init; }

  /// <summary>
  /// Moon clearance completion status
  /// </summary>
  [SerializedLabel("moon_clearance_complete")]
  public required bool MoonClearanceComplete { get; init; }

  /// <summary>
  /// Shuttle price (target variable for ML)
  /// </summary>
  [SerializedLabel("price")]
  public required decimal Price { get; init; }

  /// <summary>
  /// Company identifier (from shuttle.company_id)
  /// </summary>
  [SerializedLabel("company_id")]
  public required string CompanyId { get; init; }

  /// <summary>
  /// Shuttle identifier (from review.shuttle_id)
  /// </summary>
  [SerializedLabel("shuttle_id")]
  public required string ShuttleId { get; init; }

  // Review columns (from reviews table)

  /// <summary>
  /// Overall review score rating
  /// </summary>
  [SerializedLabel("review_scores_rating")]
  public required decimal ReviewScoresRating { get; init; }

  /// <summary>
  /// Review comfort score
  /// </summary>
  [SerializedLabel("review_scores_comfort")]
  public decimal? ReviewScoresComfort { get; init; }

  /// <summary>
  /// Review amenities score
  /// </summary>
  [SerializedLabel("review_scores_amenities")]
  public decimal? ReviewScoresAmenities { get; init; }

  /// <summary>
  /// Review trip score
  /// </summary>
  [SerializedLabel("review_scores_trip")]
  public decimal? ReviewScoresTrip { get; init; }

  /// <summary>
  /// Review crew score
  /// </summary>
  [SerializedLabel("review_scores_crew")]
  public decimal? ReviewScoresCrew { get; init; }

  /// <summary>
  /// Review location score
  /// </summary>
  [SerializedLabel("review_scores_location")]
  public decimal? ReviewScoresLocation { get; init; }

  /// <summary>
  /// Review price score
  /// </summary>
  [SerializedLabel("review_scores_price")]
  public decimal? ReviewScoresPrice { get; init; }

  /// <summary>
  /// Number of reviews
  /// </summary>
  [SerializedLabel("number_of_reviews")]
  public int? NumberOfReviews { get; init; }

  /// <summary>
  /// Reviews per month
  /// </summary>
  [SerializedLabel("reviews_per_month")]
  public decimal? ReviewsPerMonth { get; init; }

  // Company columns (from companies table)

  /// <summary>
  /// Company identifier (from company.id, duplicate of CompanyId above)
  /// </summary>
  [SerializedLabel("id")]
  public string? Id { get; init; }

  /// <summary>
  /// Company rating (0.0 to 1.0)
  /// </summary>
  [SerializedLabel("company_rating")]
  public required decimal CompanyRating { get; init; }

  /// <summary>
  /// Company location/country
  /// </summary>
  [SerializedLabel("company_location")]
  public string? CompanyLocation { get; init; }

  /// <summary>
  /// Total fleet count
  /// </summary>
  [SerializedLabel("total_fleet_count")]
  public decimal? TotalFleetCount { get; init; }

  /// <summary>
  /// IATA approval status
  /// </summary>
  [SerializedLabel("iata_approved")]
  public required bool IataApproved { get; init; }
}
