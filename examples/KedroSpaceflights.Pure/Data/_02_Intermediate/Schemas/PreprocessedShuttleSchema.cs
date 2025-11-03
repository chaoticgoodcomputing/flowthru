using Flowthru.Abstractions;

namespace KedroSpaceflights.Pure.Data._02_Intermediate.Schemas;

/// <summary>
/// Represents preprocessed shuttle data with strongly-typed fields.
/// Produced by parsing and validating raw shuttle data.
/// </summary>
public record PreprocessedShuttleSchema : IFlatSchema, IBinarySerializable, IStructuredSerializable
{
  /// <summary>
  /// Unique identifier for the shuttle.
  /// </summary>
  [SerializedLabel("id")]
  public string Id { get; init; } = null!;

  /// <summary>
  /// Type or model of the shuttle.
  /// </summary>
  [SerializedLabel("shuttle_type")]
  public string ShuttleType { get; init; } = null!;

  /// <summary>
  /// Identifier of the company operating this shuttle.
  /// </summary>
  [SerializedLabel("company_id")]
  public string CompanyId { get; init; } = null!;

  /// <summary>
  /// Number of engines.
  /// </summary>
  [SerializedLabel("engines")]
  public int Engines { get; init; }

  /// <summary>
  /// Maximum passenger capacity.
  /// </summary>
  [SerializedLabel("passenger_capacity")]
  public int PassengerCapacity { get; init; }

  /// <summary>
  /// Required crew size.
  /// </summary>
  [SerializedLabel("crew")]
  public int Crew { get; init; }

  /// <summary>
  /// Trip price.
  /// </summary>
  [SerializedLabel("price")]
  public decimal Price { get; init; }

  /// <summary>
  /// D-check completion status.
  /// </summary>
  [SerializedLabel("d_check_complete")]
  public bool DCheckComplete { get; init; }

  /// <summary>
  /// Moon clearance completion status.
  /// </summary>
  [SerializedLabel("moon_clearance_complete")]
  public bool MoonClearanceComplete { get; init; }
}
