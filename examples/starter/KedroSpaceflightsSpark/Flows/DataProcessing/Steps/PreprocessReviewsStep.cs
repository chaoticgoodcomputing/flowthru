using Flowthru.Core.Steps;
using Flowthru.DataFrames;
using Flowthru.Extensions.Spark;
using KedroSpaceflightsSpark.Data._01_Raw.Schemas;
using KedroSpaceflightsSpark.Data._02_Intermediate.Schemas;

namespace KedroSpaceflightsSpark.Flows.DataProcessing.Steps;

/// <summary>
/// Filters raw review strings to entries with parseable numeric scores and loads
/// them into a typed Spark DataFrame.
/// </summary>
[FlowthruStep]
public static class PreprocessReviewsStep
{
  public static Func<IEnumerable<ReviewSchema>, TypedFrame<ParsedReviewSchema>> Create(
    SparkFrameProvider frameProvider
  )
  {
    return (input) =>
    {
      var parsed = input.Select(Parse).Where(r => r != null).Cast<ParsedReviewSchema>();

      return frameProvider.CreateFromEnumerable(parsed);
    };
  }

  private static ParsedReviewSchema? Parse(ReviewSchema raw)
  {
    if (!double.TryParse(raw.ReviewScoresRating, out var score))
      return null;

    return new ParsedReviewSchema { ShuttleId = raw.ShuttleId, ReviewScoresRating = score };
  }
}
