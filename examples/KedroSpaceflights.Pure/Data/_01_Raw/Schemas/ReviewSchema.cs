using Flowthru.Abstractions;

namespace KedroSpaceflights.Pure.Data._01_Raw.Schemas;

public record ReviewSchema : IFlatSchema, ITextSerializable
{
  [SerializedLabel("shuttle_id")]
  public string ShuttleId { get; init; } = null!;

  [SerializedLabel("review_scores_rating")]
  public string ReviewScoresRating { get; init; } = null!;
}
