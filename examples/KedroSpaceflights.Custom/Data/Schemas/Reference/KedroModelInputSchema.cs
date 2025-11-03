using Flowthru.Abstractions;

namespace KedroSpaceflights.Custom.Data.Schemas.Reference;

/// <summary>
/// Reference model input table schema from Kedro pipeline.
/// Contains all columns from the original Kedro spaceflights-pandas starter.
/// Used for validation against Flowthru's implementation.
/// </summary>
public record KedroModelInputSchema
  : IFlatSchema,
    ITextSerializable,
    IBinarySerializable,
    IStructuredSerializable
{
  // Shuttle properties

  [SerializedLabel("shuttle_id")]
  public string? ShuttleId { get; init; }

  [SerializedLabel("shuttle_location")]
  public string? ShuttleLocation { get; init; }

  [SerializedLabel("shuttle_type")]
  public string? ShuttleType { get; init; }

  [SerializedLabel("engine_type")]
  public string? EngineType { get; init; }

  [SerializedLabel("engine_vendor")]
  public string? EngineVendor { get; init; }

  [SerializedLabel("engines")]
  public double? Engines { get; init; }

  [SerializedLabel("passenger_capacity")]
  public double? PassengerCapacity { get; init; }

  [SerializedLabel("cancellation_policy")]
  public string? CancellationPolicy { get; init; }

  [SerializedLabel("crew")]
  public double? Crew { get; init; }

  [SerializedLabel("d_check_complete")]
  public bool DCheckComplete { get; init; }

  [SerializedLabel("moon_clearance_complete")]
  public bool MoonClearanceComplete { get; init; }

  [SerializedLabel("price")]
  public decimal Price { get; init; }

  // Company properties

  [SerializedLabel("company_id")]
  public string? CompanyId { get; init; }

  [SerializedLabel("id")]
  public string? Id { get; init; }

  [SerializedLabel("company_rating")]
  public double? CompanyRating { get; init; }

  [SerializedLabel("company_location")]
  public string? CompanyLocation { get; init; }

  [SerializedLabel("total_fleet_count")]
  public double? TotalFleetCount { get; init; }

  [SerializedLabel("iata_approved")]
  public bool IataApproved { get; init; }

  // Review properties

  [SerializedLabel("review_scores_rating")]
  public decimal? ReviewScoresRating { get; init; }

  [SerializedLabel("review_scores_comfort")]
  public decimal? ReviewScoresComfort { get; init; }

  [SerializedLabel("review_scores_amenities")]
  public decimal? ReviewScoresAmenities { get; init; }

  [SerializedLabel("review_scores_trip")]
  public decimal? ReviewScoresTrip { get; init; }

  [SerializedLabel("review_scores_crew")]
  public decimal? ReviewScoresCrew { get; init; }

  [SerializedLabel("review_scores_location")]
  public decimal? ReviewScoresLocation { get; init; }

  [SerializedLabel("review_scores_price")]
  public decimal? ReviewScoresPrice { get; init; }

  [SerializedLabel("number_of_reviews")]
  public double? NumberOfReviews { get; init; }

  [SerializedLabel("reviews_per_month")]
  public decimal? ReviewsPerMonth { get; init; }
}
