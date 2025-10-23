using System.ComponentModel.DataAnnotations;
using Flowthru.Abstractions;

namespace Flowthru.Tests.KedroSpaceflights.Data.Schemas.Processed;

/// <summary>
/// Processed review data with type conversions applied and nulls removed.
/// Output of PreprocessReviewsNode.
/// </summary>
public record ReviewSchema : IFlatSerializable {
  /// <summary>
  /// Shuttle identifier (foreign key to shuttles)
  /// </summary>
  [Required]
  public string ShuttleId { get; init; } = null!;

  /// <summary>
  /// Overall review score rating
  /// </summary>
  public decimal ReviewScoresRating { get; init; }

  /// <summary>
  /// Comfort score
  /// </summary>
  public decimal ReviewScoresComfort { get; init; }

  /// <summary>
  /// Amenities score
  /// </summary>
  public decimal ReviewScoresAmenities { get; init; }

  /// <summary>
  /// Trip score
  /// </summary>
  public decimal ReviewScoresTrip { get; init; }

  /// <summary>
  /// Crew score
  /// </summary>
  public decimal ReviewScoresCrew { get; init; }

  /// <summary>
  /// Location score
  /// </summary>
  public decimal ReviewScoresLocation { get; init; }

  /// <summary>
  /// Price score
  /// </summary>
  public decimal ReviewScoresPrice { get; init; }

  /// <summary>
  /// Number of reviews
  /// </summary>
  public int NumberOfReviews { get; init; }

  /// <summary>
  /// Reviews per month
  /// </summary>
  public decimal ReviewsPerMonth { get; init; }
}
