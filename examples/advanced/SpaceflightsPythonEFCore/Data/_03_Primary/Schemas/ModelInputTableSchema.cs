using Flowthru.Abstractions;

namespace SpaceflightsPythonEFCore.Data._03_Primary.Schemas;

/// <summary>
/// Unified model input table combining shuttle, company, and review data.
/// Produced by the C# DataProcessing pipeline and consumed by Python DataScience nodes.
/// </summary>
[FlowthruSchema]
public partial record ModelInputTableSchema
{
  [SerializedLabel("shuttle_id")]
  public required string ShuttleId { get; init; }

  [SerializedLabel("shuttle_type")]
  public required string ShuttleType { get; init; }

  [SerializedLabel("engine_type")]
  public required string EngineType { get; init; }

  [SerializedLabel("company_id")]
  public required string CompanyId { get; init; }

  [SerializedLabel("engines")]
  public required int Engines { get; init; }

  [SerializedLabel("passenger_capacity")]
  public required int PassengerCapacity { get; init; }

  [SerializedLabel("crew")]
  public required int Crew { get; init; }

  [SerializedLabel("d_check_complete")]
  public required bool DCheckComplete { get; init; }

  [SerializedLabel("moon_clearance_complete")]
  public required bool MoonClearanceComplete { get; init; }

  [SerializedLabel("price")]
  public required double Price { get; init; }

  [SerializedLabel("iata_approved")]
  public required bool IataApproved { get; init; }

  [SerializedLabel("company_rating")]
  public required double CompanyRating { get; init; }

  [SerializedLabel("company_location")]
  public required string CompanyLocation { get; init; }

  [SerializedLabel("total_fleet_count")]
  public double? TotalFleetCount { get; init; }

  [SerializedLabel("review_scores_rating")]
  public required double ReviewScoresRating { get; init; }
}
