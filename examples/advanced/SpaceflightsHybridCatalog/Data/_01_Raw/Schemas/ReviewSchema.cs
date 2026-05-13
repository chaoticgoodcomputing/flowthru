using Flowthru.Data.Schema;

namespace SpaceflightsHybridCatalog.Data._01_Raw.Schemas;

/// <summary>
/// Represents raw review data as imported from text files.
/// All fields are stored as strings pending parsing.
/// </summary>
[FlowthruSchema]
public partial record ReviewSchema
{
  [SerializedLabel("shuttle_id")]
  public string ShuttleId { get; init; } = null!;

  [SerializedLabel("review_scores_rating")]
  public string ReviewScoresRating { get; init; } = null!;
}
