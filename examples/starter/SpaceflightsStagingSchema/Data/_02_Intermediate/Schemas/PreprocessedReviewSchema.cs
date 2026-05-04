using Flowthru.Core.Abstractions;

namespace SpaceflightsStagingSchema.Data._02_Intermediate.Schemas;

/// <summary>
/// Preprocessed review data with a decimal score parsed from the raw string form.
/// </summary>
[FlowthruSchema]
public partial record PreprocessedReviewSchema
{
  [SerializedLabel("shuttle_id")]
  public required string ShuttleId { get; init; }

  [SerializedLabel("review_scores_rating")]
  public required decimal ReviewScoresRating { get; init; }
}
