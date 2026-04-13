using Flowthru.Core.Steps;
using KedroSpaceflightsGQL.Data._01_Raw.Schemas;
using KedroSpaceflightsGQL.Data._02_Intermediate.Schemas;

namespace KedroSpaceflightsGQL.Flows.Ingest.Steps;

/// <summary>
/// Preprocesses raw review data by parsing the rating string to a decimal score.
/// Records with unparseable scores are dropped.
/// </summary>
[FlowthruStep]
public static class PreprocessReviewsStep
{
  /// <summary>
  /// Creates a preprocessing function that transforms raw review records into strongly-typed records.
  /// </summary>
  public static Func<
    IEnumerable<ReviewSchema>,
    IEnumerable<PreprocessedReviewSchema>
  > Create() =>
    input =>
      input
        .Select(raw => Parse(raw))
        .Where(item => item != null)
        .Cast<PreprocessedReviewSchema>();

  private static PreprocessedReviewSchema? Parse(ReviewSchema raw)
  {
    if (!decimal.TryParse(raw.ReviewScoresRating, out var score))
      return null;

    return new PreprocessedReviewSchema { ShuttleId = raw.ShuttleId, ReviewScoresRating = score };
  }
}
