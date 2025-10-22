using CsvHelper.Configuration.Attributes;

namespace Flowthru.Tests.KedroSpaceflights.Data.Schemas.Reference;

/// <summary>
/// Reference model input table schema from Kedro pipeline.
/// Contains all columns from the original Kedro spaceflights-pandas starter.
/// Used for validation against Flowthru's implementation.
/// </summary>
public record KedroModelInputSchema {
  // Shuttle properties

  [Name("shuttle_id")]
  public string? ShuttleId { get; init; }

  [Name("shuttle_location")]
  public string? ShuttleLocation { get; init; }

  [Name("shuttle_type")]
  public string? ShuttleType { get; init; }

  [Name("engine_type")]
  public string? EngineType { get; init; }

  [Name("engine_vendor")]
  public string? EngineVendor { get; init; }

  [Name("engines")]
  public double? Engines { get; init; }

  [Name("passenger_capacity")]
  public double? PassengerCapacity { get; init; }

  [Name("cancellation_policy")]
  public string? CancellationPolicy { get; init; }

  [Name("crew")]
  public double? Crew { get; init; }

  [Name("d_check_complete")]
  public bool DCheckComplete { get; init; }

  [Name("moon_clearance_complete")]
  public bool MoonClearanceComplete { get; init; }

  [Name("price")]
  public decimal Price { get; init; }

  // Company properties

  [Name("company_id")]
  public string? CompanyId { get; init; }

  [Name("id")]
  public string? Id { get; init; }

  [Name("company_rating")]
  public double? CompanyRating { get; init; }

  [Name("company_location")]
  public string? CompanyLocation { get; init; }

  [Name("total_fleet_count")]
  public double? TotalFleetCount { get; init; }

  [Name("iata_approved")]
  public bool IataApproved { get; init; }

  // Review properties

  [Name("review_scores_rating")]
  public decimal? ReviewScoresRating { get; init; }

  [Name("review_scores_comfort")]
  public decimal? ReviewScoresComfort { get; init; }

  [Name("review_scores_amenities")]
  public decimal? ReviewScoresAmenities { get; init; }

  [Name("review_scores_trip")]
  public decimal? ReviewScoresTrip { get; init; }

  [Name("review_scores_crew")]
  public decimal? ReviewScoresCrew { get; init; }

  [Name("review_scores_location")]
  public decimal? ReviewScoresLocation { get; init; }

  [Name("review_scores_price")]
  public decimal? ReviewScoresPrice { get; init; }

  [Name("number_of_reviews")]
  public double? NumberOfReviews { get; init; }

  [Name("reviews_per_month")]
  public decimal? ReviewsPerMonth { get; init; }
}
