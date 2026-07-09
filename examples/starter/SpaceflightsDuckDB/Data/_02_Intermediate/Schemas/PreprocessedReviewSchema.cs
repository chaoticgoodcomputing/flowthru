using Flowthru.Data.Schema;

namespace SpaceflightsDuckDB.Data._02_Intermediate.Schemas;

/// <summary>
/// Represents preprocessed review data with a strongly-typed score.
/// Produced by parsing and validating raw review data.
/// </summary>
/// <remarks>
/// Per-row parsing like this stays in C#; only the typed result crosses into the
/// engine-side SQL steps. Uses required members to enforce that all critical
/// fields must be set during construction.
/// </remarks>
[FlowthruSchema]
public partial record PreprocessedReviewSchema
{
  /// <summary>
  /// Identifier of the shuttle being reviewed.
  /// </summary>
  [SerializedLabel("shuttle_id")]
  public required string ShuttleId { get; init; }

  /// <summary>
  /// Review rating score, parsed from the raw string field.
  /// </summary>
  [SerializedLabel("review_scores_rating")]
  public required decimal ReviewScoresRating { get; init; }
}
