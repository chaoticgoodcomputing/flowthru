using Flowthru.Core.Abstractions;

namespace SpaceflightsDistributed.DataProcessing.Data._01_Raw.Schemas;

[FlowthruSchema]
public partial record ReviewSchema
{
  [SerializedLabel("shuttle_id")]
  public string ShuttleId { get; init; } = null!;

  [SerializedLabel("review_scores_rating")]
  public string ReviewScoresRating { get; init; } = null!;
}
