using Flowthru.Abstractions;

namespace KedroSpaceflights.Pure.Data._02_Intermediate.Schemas;

public record PreprocessedShuttleSchema : IFlatSchema, IBinarySerializable, IStructuredSerializable
{
  [SerializedLabel("id")]
  public string Id { get; init; } = null!;

  [SerializedLabel("shuttle_type")]
  public string ShuttleType { get; init; } = null!;

  [SerializedLabel("company_id")]
  public string CompanyId { get; init; } = null!;

  [SerializedLabel("engines")]
  public int Engines { get; init; }

  [SerializedLabel("passenger_capacity")]
  public int PassengerCapacity { get; init; }

  [SerializedLabel("crew")]
  public int Crew { get; init; }

  [SerializedLabel("price")]
  public decimal Price { get; init; }

  [SerializedLabel("d_check_complete")]
  public bool DCheckComplete { get; init; }

  [SerializedLabel("moon_clearance_complete")]
  public bool MoonClearanceComplete { get; init; }
}
