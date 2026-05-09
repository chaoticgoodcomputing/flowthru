using Flowthru.Data.Schema;

namespace SpaceflightsDistributed.DataProcessing.Data._02_Intermediate.Schemas;

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
  public required bool DCheckComplete { get; init; }

  [SerializedLabel("moon_clearance_complete")]
  public required bool MoonClearanceComplete { get; init; }
}
