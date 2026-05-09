using Flowthru.Data.Schema;

namespace SpaceflightsNewTypes.Data._01_Raw.Schemas;

/// <summary>
/// Represents raw review data as imported from text files.
/// All fields are stored as strings pending parsing.
/// </summary>
/// <remarks>
/// References <c>ShuttleId</c> declared in <see cref="ShuttleSchema"/>. The compiler
/// guarantees that joining reviews to shuttles on these keys cannot accidentally cross
/// in a <c>CompanyId</c>.
/// </remarks>
[FlowthruSchema]
public partial record ReviewSchema
{
  /// <summary>
  /// Identifier of the shuttle being reviewed.
  /// </summary>
  [FlowthruColumn(typeof(string))]
  [SerializedLabel("shuttle_id")]
  public required ShuttleId ShuttleId { get; init; }

  /// <summary>
  /// Review rating score as a string.
  /// </summary>
  [SerializedLabel("review_scores_rating")]
  public string ReviewScoresRating { get; init; } = null!;
}
