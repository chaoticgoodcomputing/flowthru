using Flowthru.Data.Schema;

namespace SpaceflightsPythonEFCore.Data._01_Raw.Schemas;

/// <summary>
/// Represents raw shuttle data as imported from structured files.
/// All fields are stored as strings pending parsing.
/// </summary>
[FlowthruSchema]
public partial record ShuttleSchema
{
  [SerializedLabel("id")]
  public string Id { get; init; } = null!;

  [SerializedLabel("shuttle_location")]
  public string ShuttleLocation { get; init; } = null!;

  [SerializedLabel("shuttle_type")]
  public string ShuttleType { get; init; } = null!;

  [SerializedLabel("engine_type")]
  public string EngineType { get; init; } = null!;

  [SerializedLabel("engine_vendor")]
  public string EngineVendor { get; init; } = null!;

  [SerializedLabel("company_id")]
  public string CompanyId { get; init; } = null!;

  [SerializedLabel("engines")]
  public string Engines { get; init; } = null!;

  [SerializedLabel("passenger_capacity")]
  public string PassengerCapacity { get; init; } = null!;

  [SerializedLabel("crew")]
  public string Crew { get; init; } = null!;

  [SerializedLabel("price")]
  public string Price { get; init; } = null!;

  [SerializedLabel("d_check_complete")]
  public string DCheckComplete { get; init; } = null!;

  [SerializedLabel("moon_clearance_complete")]
  public string MoonClearanceComplete { get; init; } = null!;
}
