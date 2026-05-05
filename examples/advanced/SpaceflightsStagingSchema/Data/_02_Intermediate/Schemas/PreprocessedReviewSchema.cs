using Flowthru.Core.Abstractions;

namespace SpaceflightsStagingSchema.Data._02_Intermediate.Schemas;

/// <summary>
/// Preprocessed review data with a decimal score parsed from the raw string form.
/// </summary>
[FlowthruSchema]
public partial record PreprocessedReviewSchema
{
  /// <summary>
  /// Auto-generated surrogate key. Reviews have no natural primary key in the
  /// raw data, so the database assigns one on insert. Declared explicitly
  /// (rather than as an EF shadow property) because <c>EFCore.BulkExtensions</c>
  /// does not support shadow primary keys.
  /// </summary>
  public int Id { get; init; }

  [SerializedLabel("shuttle_id")]
  public required string ShuttleId { get; init; }

  [SerializedLabel("review_scores_rating")]
  public required decimal ReviewScoresRating { get; init; }
}
