using Flowthru.Core.Abstractions;

namespace KedroSpaceflightsSpark.Data._03_Primary.Schemas;

/// <summary>
/// Intermediate join result combining preprocessed shuttle data with a review score.
/// Produced by joining TypedFrame&lt;PreprocessedShuttleSchema&gt; with
/// TypedFrame&lt;ParsedReviewSchema&gt;; consumed by the second join that adds company fields.
/// </summary>
[FlowthruSchema]
public partial record ShuttleReviewSchema
{
    [SerializedLabel("shuttle_id")]
    public required string ShuttleId { get; init; }

    [SerializedLabel("shuttle_type")]
    public required string ShuttleType { get; init; }

    [SerializedLabel("company_id")]
    public required string CompanyId { get; init; }

    [SerializedLabel("engines")]
    public required int Engines { get; init; }

    [SerializedLabel("passenger_capacity")]
    public required int PassengerCapacity { get; init; }

    [SerializedLabel("crew")]
    public required int Crew { get; init; }

    [SerializedLabel("price")]
    public required double Price { get; init; }

    [SerializedLabel("d_check_complete")]
    public required bool DCheckComplete { get; init; }

    [SerializedLabel("moon_clearance_complete")]
    public required bool MoonClearanceComplete { get; init; }

    [SerializedLabel("review_scores_rating")]
    public required double ReviewScoresRating { get; init; }
}
