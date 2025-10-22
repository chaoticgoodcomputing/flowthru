using Flowthru.Nodes;
using Flowthru.Spaceflights.Data.Schemas.Raw;
using Flowthru.Spaceflights.Data.Schemas.Processed;

namespace Flowthru.Spaceflights.Pipelines.DataProcessing.Nodes;

/// <summary>
/// Preprocesses raw review data by converting string values to proper types and filtering out incomplete records.
/// Converts string scores to decimals and drops rows with missing required fields.
/// 
/// Stateless node with implicit parameterless constructor,
/// compatible with type reference instantiation for distributed/parallel execution.
/// </summary>
public class PreprocessReviewsNode : NodeBase<ReviewRawSchema, ReviewSchema>
{
  protected override Task<IEnumerable<ReviewSchema>> Transform(
      IEnumerable<ReviewRawSchema> input)
  {
    var processed = input
        .Select(r => TryParse(r))
        .Where(r => r != null)
        .Cast<ReviewSchema>();

    return Task.FromResult(processed);
  }

  /// <summary>
  /// Attempts to parse a raw review record into a processed review.
  /// Returns null if any required field is missing or invalid.
  /// </summary>
  private static ReviewSchema? TryParse(ReviewRawSchema raw)
  {
    // Parse all fields
    var reviewScoresRating = ParseDecimal(raw.ReviewScoresRating);
    var reviewScoresComfort = ParseDecimal(raw.ReviewScoresComfort);
    var reviewScoresAmenities = ParseDecimal(raw.ReviewScoresAmenities);
    var reviewScoresTrip = ParseDecimal(raw.ReviewScoresTrip);
    var reviewScoresCrew = ParseDecimal(raw.ReviewScoresCrew);
    var reviewScoresLocation = ParseDecimal(raw.ReviewScoresLocation);
    var reviewScoresPrice = ParseDecimal(raw.ReviewScoresPrice);
    var numberOfReviews = ParseInt(raw.NumberOfReviews);
    var reviewsPerMonth = ParseDecimal(raw.ReviewsPerMonth);

    // Validation: all fields must be present
    if (string.IsNullOrWhiteSpace(raw.ShuttleId)
        || !reviewScoresRating.HasValue
        || !reviewScoresComfort.HasValue
        || !reviewScoresAmenities.HasValue
        || !reviewScoresTrip.HasValue
        || !reviewScoresCrew.HasValue
        || !reviewScoresLocation.HasValue
        || !reviewScoresPrice.HasValue
        || !numberOfReviews.HasValue
        || !reviewsPerMonth.HasValue)
    {
      return null; // Parse failed - incomplete record
    }

    // Parse succeeded - return validated, non-nullable type
    return new ReviewSchema
    {
      ShuttleId = raw.ShuttleId,
      ReviewScoresRating = reviewScoresRating.Value,
      ReviewScoresComfort = reviewScoresComfort.Value,
      ReviewScoresAmenities = reviewScoresAmenities.Value,
      ReviewScoresTrip = reviewScoresTrip.Value,
      ReviewScoresCrew = reviewScoresCrew.Value,
      ReviewScoresLocation = reviewScoresLocation.Value,
      ReviewScoresPrice = reviewScoresPrice.Value,
      NumberOfReviews = numberOfReviews.Value,
      ReviewsPerMonth = reviewsPerMonth.Value
    };
  }

  /// <summary>
  /// Parses decimal from string, returns null if empty/invalid
  /// </summary>
  private static decimal? ParseDecimal(string? value)
  {
    if (string.IsNullOrWhiteSpace(value))
      return null;

    if (decimal.TryParse(value, out var result))
      return result;

    return null;
  }

  /// <summary>
  /// Parses integer from string, returns null if empty/invalid
  /// </summary>
  private static int? ParseInt(string? value)
  {
    if (string.IsNullOrWhiteSpace(value))
      return null;

    if (int.TryParse(value, out var result))
      return result;

    return null;
  }
}
