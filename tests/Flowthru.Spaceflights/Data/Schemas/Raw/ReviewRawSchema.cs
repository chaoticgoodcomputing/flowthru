namespace Flowthru.Spaeflights.Data.Schemas.Raw;

/// <summary>
/// Raw review data as read from CSV file.
/// Matches structure of Datasets/01_Raw/reviews.csv
/// </summary>
public record ReviewRawSchema
{
  /// <summary>
  /// Shuttle identifier (foreign key to shuttles)
  /// </summary>
  public required string ShuttleId { get; init; }

  /// <summary>
  /// Overall review score rating
  /// </summary>
  public string? ReviewScoresRating { get; init; }

  /// <summary>
  /// Comfort score
  /// </summary>
  public string? ReviewScoresComfort { get; init; }

  /// <summary>
  /// Amenities score
  /// </summary>
  public string? ReviewScoresAmenities { get; init; }

  /// <summary>
  /// Trip score
  /// </summary>
  public string? ReviewScoresTrip { get; init; }

  /// <summary>
  /// Crew score
  /// </summary>
  public string? ReviewScoresCrew { get; init; }

  /// <summary>
  /// Location score
  /// </summary>
  public string? ReviewScoresLocation { get; init; }

  /// <summary>
  /// Price score
  /// </summary>
  public string? ReviewScoresPrice { get; init; }

  /// <summary>
  /// Number of reviews
  /// </summary>
  public string? NumberOfReviews { get; init; }

  /// <summary>
  /// Reviews per month
  /// </summary>
  public string? ReviewsPerMonth { get; init; }
}
