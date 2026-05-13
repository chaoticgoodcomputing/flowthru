using Flowthru.Data.Schema;
using SpaceflightsHybridCatalog.Data._02_Intermediate.Schemas;

namespace SpaceflightsHybridCatalog.Data._03_Primary.Schemas;

/// <summary>
/// Unified model input table combining shuttle, company, and review data.
/// Produced by joining preprocessed shuttle and company data with review scores.
/// </summary>
[FlowthruSchema]
public partial record ModelInputTableSchema
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

  [SerializedLabel("d_check_complete")]
  public required CheckStatus DCheckComplete { get; init; }

  [SerializedLabel("moon_clearance_complete")]
  public required CheckStatus MoonClearanceComplete { get; init; }

  [SerializedLabel("price")]
  public required decimal Price { get; init; }

  [SerializedLabel("iata_approved")]
  public required bool IataApproved { get; init; }

  [SerializedLabel("company_rating")]
  public required decimal CompanyRating { get; init; }

  [SerializedLabel("review_scores_rating")]
  public required decimal ReviewScoresRating { get; init; }
}
