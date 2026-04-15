using Flowthru.Core.Abstractions;

namespace KedroSpaceflights.Custom.Data._01_Raw.Schemas;

/// <summary>
/// Raw review data as read from CSV file.
/// Matches structure of Datasets/01_Raw/reviews.csv
/// </summary>
[FlowthruSchema]
public partial record ReviewRawSchema
{
    /// <summary>
    /// Shuttle identifier (foreign key to shuttles)
    /// </summary>
    [SerializedLabel("shuttle_id")]
    public required string ShuttleId { get; init; }

    /// <summary>
    /// Overall review score rating
    /// </summary>
    [SerializedLabel("review_scores_rating")]
    public string? ReviewScoresRating { get; init; }

    /// <summary>
    /// Comfort score
    /// </summary>
    [SerializedLabel("review_scores_comfort")]
    public string? ReviewScoresComfort { get; init; }

    /// <summary>
    /// Amenities score
    /// </summary>
    [SerializedLabel("review_scores_amenities")]
    public string? ReviewScoresAmenities { get; init; }

    /// <summary>
    /// Trip score
    /// </summary>
    [SerializedLabel("review_scores_trip")]
    public string? ReviewScoresTrip { get; init; }

    /// <summary>
    /// Crew score
    /// </summary>
    [SerializedLabel("review_scores_crew")]
    public string? ReviewScoresCrew { get; init; }

    /// <summary>
    /// Location score
    /// </summary>
    [SerializedLabel("review_scores_location")]
    public string? ReviewScoresLocation { get; init; }

    /// <summary>
    /// Price score
    /// </summary>
    [SerializedLabel("review_scores_price")]
    public string? ReviewScoresPrice { get; init; }

    /// <summary>
    /// Number of reviews
    /// </summary>
    [SerializedLabel("number_of_reviews")]
    public string? NumberOfReviews { get; init; }

    /// <summary>
    /// Reviews per month
    /// </summary>
    [SerializedLabel("reviews_per_month")]
    public string? ReviewsPerMonth { get; init; }
}
