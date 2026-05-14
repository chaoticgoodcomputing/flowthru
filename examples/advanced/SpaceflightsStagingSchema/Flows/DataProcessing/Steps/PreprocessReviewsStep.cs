using Flowthru.Step;
using SpaceflightsStagingSchema.Data._01_Raw.Schemas;
using SpaceflightsStagingSchema.Data._02_Intermediate.Schemas;

namespace SpaceflightsStagingSchema.Flows.DataProcessing.Steps;

/// <summary>
/// Parses raw reviews into typed records. Rows with non-numeric scores are dropped.
/// </summary>
[FlowthruStep]
public static class PreprocessReviewsStep
{
  public static Func<
    (IEnumerable<ReviewSchema>, SeedingOptions),
    IEnumerable<PreprocessedReviewSchema>
  > Create() => input =>
  {
    var (raw, options) = input;
    var real = raw.Select(Parse).Where(item => item is not null).Cast<PreprocessedReviewSchema>();
    var synthetic = SyntheticDataSeeder.Reviews(
      options.SyntheticReviews,
      options.SyntheticShuttles,
      options.RandomSeed
    );
    return real.Concat(synthetic);
  };

  private static PreprocessedReviewSchema? Parse(ReviewSchema raw)
  {
    if (string.IsNullOrWhiteSpace(raw.ShuttleId))
      return null;
    if (!decimal.TryParse(raw.ReviewScoresRating, out var score))
      return null;

    return new PreprocessedReviewSchema
    {
      ShuttleId = raw.ShuttleId,
      ReviewScoresRating = score,
    };
  }
}
