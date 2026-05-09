using Flowthru.Data.Schema;

namespace SpaceflightsPythonEFCore.Data._02_Intermediate.Schemas;

/// <summary>
/// Preprocessed shuttle data with strongly-typed fields, produced by the C# DataProcessing pipeline.
/// </summary>
[FlowthruSchema]
public partial record PreprocessedShuttleSchema
{
  [SerializedLabel("id")]
  public required int Id { get; init; }

  [SerializedLabel("shuttle_type")]
  public required string ShuttleType { get; init; }

  [SerializedLabel("engine_type")]
  public required string EngineType { get; init; }

  [SerializedLabel("company_id")]
  public required int CompanyId { get; init; }

  [SerializedLabel("engines")]
  public required int Engines { get; init; }

  [SerializedLabel("passenger_capacity")]
  public required int PassengerCapacity { get; init; }

  [SerializedLabel("crew")]
  public required int Crew { get; init; }

  [SerializedLabel("price")]
  public required double Price { get; init; }

  [SerializedLabel("d_check_complete")]
  public required bool DCheckComplete { get; init; }

  [SerializedLabel("moon_clearance_complete")]
  public required bool MoonClearanceComplete { get; init; }
}
