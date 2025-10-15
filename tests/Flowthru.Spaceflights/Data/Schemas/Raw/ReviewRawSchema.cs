using CsvHelper.Configuration.Attributes;

namespace Flowthru.Spaceflights.Data.Schemas.Raw;

/// <summary>
/// Raw review data as read from CSV file.
/// Matches structure of Datasets/01_Raw/reviews.csv
/// </summary>
public record ReviewRawSchema
{
  /// <summary>
  /// Shuttle identifier (foreign key to shuttles)
  /// </summary>
  [Name("shuttle_id")]
  public required string ShuttleId { get; init; }

  /// <summary>
  /// Overall review score rating
  /// </summary>
  [Name("review_scores_rating")]
  public string? ReviewScoresRating { get; init; }

  /// <summary>
  /// Comfort score
  /// </summary>
  [Name("review_scores_comfort")]
  public string? ReviewScoresComfort { get; init; }

  /// <summary>
  /// Amenities score
  /// </summary>
  [Name("review_scores_amenities")]
  public string? ReviewScoresAmenities { get; init; }

  /// <summary>
  /// Trip score
  /// </summary>
  [Name("review_scores_trip")]
  public string? ReviewScoresTrip { get; init; }

  /// <summary>
  /// Crew score
  /// </summary>
  [Name("review_scores_crew")]
  public string? ReviewScoresCrew { get; init; }

  /// <summary>
  /// Location score
  /// </summary>
  [Name("review_scores_location")]
  public string? ReviewScoresLocation { get; init; }

  /// <summary>
  /// Price score
  /// </summary>
  [Name("review_scores_price")]
  public string? ReviewScoresPrice { get; init; }

  /// <summary>
  /// Number of reviews
  /// </summary>
  [Name("number_of_reviews")]
  public string? NumberOfReviews { get; init; }

  /// <summary>
  /// Reviews per month
  /// </summary>
  [Name("reviews_per_month")]
  public string? ReviewsPerMonth { get; init; }
}
