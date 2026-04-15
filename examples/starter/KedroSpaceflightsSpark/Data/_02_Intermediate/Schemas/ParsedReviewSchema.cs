using Flowthru.Core.Abstractions;

namespace KedroSpaceflightsSpark.Data._02_Intermediate.Schemas;

/// <summary>
/// Review data filtered to entries with valid numeric scores.
/// Produced by PreprocessReviewsStep; passed into the model input table join as a TypedFrame.
/// </summary>
[FlowthruSchema]
public partial record ParsedReviewSchema
{
    [SerializedLabel("shuttle_id")]
    public required string ShuttleId { get; init; }

    [SerializedLabel("review_scores_rating")]
    public required double ReviewScoresRating { get; init; }
}
