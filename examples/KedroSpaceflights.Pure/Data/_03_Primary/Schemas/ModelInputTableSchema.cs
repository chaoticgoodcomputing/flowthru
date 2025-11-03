using Flowthru.Abstractions;

namespace KedroSpaceflights.Pure.Data._03_Primary.Schemas;

public record ModelInputTableSchema : IFlatSchema, IBinarySerializable, IStructuredSerializable
{
  [SerializedLabel("shuttle_id")]
  public string ShuttleId { get; init; } = null!;

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

  [SerializedLabel("d_check_complete")]
  public bool DCheckComplete { get; init; }

  [SerializedLabel("moon_clearance_complete")]
  public bool MoonClearanceComplete { get; init; }

  [SerializedLabel("price")]
  public decimal Price { get; init; }

  [SerializedLabel("iata_approved")]
  public bool IataApproved { get; init; }

  [SerializedLabel("company_rating")]
  public decimal CompanyRating { get; init; }

  [SerializedLabel("review_scores_rating")]
  public decimal ReviewScoresRating { get; init; }
}
