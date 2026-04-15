using Flowthru.Core.Abstractions;
using Flowthru.Core.Data;

namespace KedroSpaceflights.Custom.Data._01_Raw.Schemas;

/// <summary>
/// Reference model input table schema from Kedro pipeline.
/// Contains all columns from the original Kedro spaceflights-pandas starter.
/// Used for validation against Flowthru's implementation.
/// </summary>
[FlowthruSchema]
public partial record KedroModelInputSchema
{
    // Shuttle properties

    [SerializedLabel("shuttle_id")]
    public required string ShuttleId { get; init; }

    [SerializedLabel("shuttle_location")]
    public required string ShuttleLocation { get; init; }

    [SerializedLabel("shuttle_type")]
    public required string ShuttleType { get; init; }

    [SerializedLabel("engine_type")]
    public required string EngineType { get; init; }

    [SerializedLabel("engine_vendor")]
    public required string EngineVendor { get; init; }

    [SerializedLabel("engines")]
    public required double Engines { get; init; }

    [SerializedLabel("passenger_capacity")]
    public required double PassengerCapacity { get; init; }

    [SerializedLabel("cancellation_policy")]
    public required string CancellationPolicy { get; init; }

    [SerializedLabel("crew")]
    public required double Crew { get; init; }

    [SerializedLabel("d_check_complete")]
    public required bool DCheckComplete { get; init; }

    [SerializedLabel("moon_clearance_complete")]
    public required bool MoonClearanceComplete { get; init; }

    [SerializedLabel("price")]
    public required decimal Price { get; init; }

    // Company properties

    [SerializedLabel("company_id")]
    public required string CompanyId { get; init; }

    [SerializedLabel("id")]
    public required string Id { get; init; }

    [SerializedLabel("company_rating")]
    public required double CompanyRating { get; init; }

    [SerializedLabel("company_location")]
    public required string CompanyLocation { get; init; }

    [SerializedLabel("total_fleet_count")]
    public required double TotalFleetCount { get; init; }

    [SerializedLabel("iata_approved")]
    public required bool IataApproved { get; init; }

    // Review properties

    [SerializedLabel("review_scores_rating")]
    public required decimal ReviewScoresRating { get; init; }

    [SerializedLabel("review_scores_comfort")]
    public required decimal ReviewScoresComfort { get; init; }

    [SerializedLabel("review_scores_amenities")]
    public required decimal ReviewScoresAmenities { get; init; }

    [SerializedLabel("review_scores_trip")]
    public required decimal ReviewScoresTrip { get; init; }

    [SerializedLabel("review_scores_crew")]
    public required decimal ReviewScoresCrew { get; init; }

    [SerializedLabel("review_scores_location")]
    public required decimal ReviewScoresLocation { get; init; }

    [SerializedLabel("review_scores_price")]
    public required decimal ReviewScoresPrice { get; init; }

    [SerializedLabel("number_of_reviews")]
    public required double NumberOfReviews { get; init; }

    [SerializedLabel("reviews_per_month")]
    public required decimal ReviewsPerMonth { get; init; }
}
