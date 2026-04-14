using Flowthru.Core.Steps;
using Flowthru.DataFrames;
using Flowthru.Extensions.Spark;
using Flowthru.Spark.Sql;
using Flowthru.Spark.Sql.Types;
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
    SparkFrameProvider provider,
    SparkSession session
  )
  {
    return (input) =>
    {
      var parsed = input
        .Select(r => Parse(r))
        .Where(r => r != null)
        .Cast<ParsedReviewSchema>()
        .ToList();

      var schema = new StructType([
        new StructField("shuttle_id", new StringType()),
        new StructField("review_scores_rating", new DoubleType()),
      ]);

      var rows = parsed.Select(r => new GenericRow([r.ShuttleId, r.ReviewScoresRating]));
      var df = session.CreateDataFrame(rows, schema);
      return provider.CreateFromNative<ParsedReviewSchema>(df);
    };
  }

  private static ParsedReviewSchema? Parse(ReviewSchema raw)
  {
    if (!double.TryParse(raw.ReviewScoresRating, out var score))
      return null;

    return new ParsedReviewSchema
    {
      ShuttleId = raw.ShuttleId,
      ReviewScoresRating = score,
    };
  }
}
