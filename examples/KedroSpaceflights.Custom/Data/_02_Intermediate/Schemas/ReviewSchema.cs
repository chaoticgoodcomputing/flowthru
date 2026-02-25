using Flowthru.Abstractions;

namespace KedroSpaceflights.Custom.Data._02_Intermediate.Schemas;

/// <summary>
/// Processed review data with type conversions applied and nulls removed.
/// Output of PreprocessReviewsNode.
/// </summary>
public record ReviewSchema
  : IFlatSchema,
    ITextSerializable,
    IBinarySerializable,
    IStructuredSerializable
{
  /// <summary>
  /// Shuttle identifier (foreign key to shuttles)
  /// </summary>
  public required string ShuttleId { get; init; }

  /// <summary>
  /// Overall review score rating
  /// </summary>
  public required decimal ReviewScoresRating { get; init; }

  /// <summary>
  /// Comfort score
  /// </summary>
  public required decimal ReviewScoresComfort { get; init; }

  /// <summary>
  /// Amenities score
  /// </summary>
  public required decimal ReviewScoresAmenities { get; init; }

  /// <summary>
  /// Trip score
  /// </summary>
  public required decimal ReviewScoresTrip { get; init; }

  /// <summary>
  /// Crew score
  /// </summary>
  public required decimal ReviewScoresCrew { get; init; }

  /// <summary>
  /// Location score
  /// </summary>
  public required decimal ReviewScoresLocation { get; init; }

  /// <summary>
  /// Price score
  /// </summary>
  public required decimal ReviewScoresPrice { get; init; }

  /// <summary>
  /// Number of reviews
  /// </summary>
  public required int NumberOfReviews { get; init; }

  /// <summary>
  /// Reviews per month
  /// </summary>
  public required decimal ReviewsPerMonth { get; init; }
}
