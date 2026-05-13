using Flowthru.Data.Schema;

namespace SpaceflightsHybridCatalog.Data._02_Intermediate.Schemas;

/// <summary>
/// Represents preprocessed shuttle data with strongly-typed fields.
/// Produced by parsing and validating raw shuttle data.
/// </summary>
[FlowthruSchema]
public partial record PreprocessedShuttleSchema
{
  [SerializedLabel("id")]
  public required string Id { get; init; }

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
  public required decimal Price { get; init; }

  [SerializedLabel("d_check_complete")]
  public required CheckStatus DCheckComplete { get; init; }

  [SerializedLabel("moon_clearance_complete")]
  public required CheckStatus MoonClearanceComplete { get; init; }
}
