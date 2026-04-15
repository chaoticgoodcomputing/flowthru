using Flowthru.Core.Abstractions;
using Flowthru.Core.Data;

namespace KedroSpaceflights.Custom.Data._03_Primary.Schemas;

/// <summary>
/// Final model input data schema (result of joining companies, shuttles, and reviews).
/// Output of CreateModelInputStep.
/// </summary>
[FlowthruSchema]
public partial record ModelInputSchema
{
  // Shuttle columns (from shuttles table)

  /// <summary>
  /// Shuttle location/origin
  /// </summary>
  [SerializedLabel("shuttle_location")]
  public required string ShuttleLocation { get; init; }

  /// <summary>
  /// Shuttle type
  /// </summary>
  [SerializedLabel("shuttle_type")]
  public required string ShuttleType { get; init; }

  /// <summary>
  /// Engine type (e.g., Plasma, Quantum)
  /// </summary>
  [SerializedLabel("engine_type")]
  public required string EngineType { get; init; }

  /// <summary>
  /// Engine vendor/manufacturer
  /// </summary>
  [SerializedLabel("engine_vendor")]
  public required string EngineVendor { get; init; }

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
  public required string CancellationPolicy { get; init; }

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
  public required decimal ReviewScoresComfort { get; init; }

  /// <summary>
  /// Review amenities score
  /// </summary>
  [SerializedLabel("review_scores_amenities")]
  public required decimal ReviewScoresAmenities { get; init; }

  /// <summary>
  /// Review trip score
  /// </summary>
  [SerializedLabel("review_scores_trip")]
  public required decimal ReviewScoresTrip { get; init; }

  /// <summary>
  /// Review crew score
  /// </summary>
  [SerializedLabel("review_scores_crew")]
  public required decimal ReviewScoresCrew { get; init; }

  /// <summary>
  /// Review location score
  /// </summary>
  [SerializedLabel("review_scores_location")]
  public required decimal ReviewScoresLocation { get; init; }

  /// <summary>
  /// Review price score
  /// </summary>
  [SerializedLabel("review_scores_price")]
  public required decimal ReviewScoresPrice { get; init; }

  /// <summary>
  /// Number of reviews
  /// </summary>
  [SerializedLabel("number_of_reviews")]
  public required int NumberOfReviews { get; init; }

  /// <summary>
  /// Reviews per month
  /// </summary>
  [SerializedLabel("reviews_per_month")]
  public required decimal ReviewsPerMonth { get; init; }

  // Company columns (from companies table)

  /// <summary>
  /// Company identifier (from company.id, duplicate of CompanyId above)
  /// </summary>
  [SerializedLabel("id")]
  public required string Id { get; init; }

  /// <summary>
  /// Company rating (0.0 to 1.0)
  /// </summary>
  [SerializedLabel("company_rating")]
  public required decimal CompanyRating { get; init; }

  /// <summary>
  /// Company location/country
  /// </summary>
  [SerializedLabel("company_location")]
  public required string CompanyLocation { get; init; }

  /// <summary>
  /// Total fleet count
  /// </summary>
  [SerializedLabel("total_fleet_count")]
  public required decimal TotalFleetCount { get; init; }

  /// <summary>
  /// IATA approval status
  /// </summary>
  [SerializedLabel("iata_approved")]
  public required bool IataApproved { get; init; }
}
