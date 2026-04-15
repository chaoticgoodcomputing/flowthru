using Flowthru.Core.Abstractions;

namespace KedroSpaceflightsGQL.Data._02_Intermediate.Schemas;

/// <summary>
/// Represents a preprocessed review with a parsed numeric rating.
/// Produced by filtering out records with unparseable rating strings.
/// </summary>
[FlowthruSchema]
public partial record PreprocessedReviewSchema
{
    /// <summary>
    /// Identifier of the shuttle being reviewed.
    /// </summary>
    [SerializedLabel("shuttle_id")]
    public required string ShuttleId { get; init; }

    /// <summary>
    /// Review rating score as a decimal value.
    /// </summary>
    [SerializedLabel("review_scores_rating")]
    public required decimal ReviewScoresRating { get; init; }
}
