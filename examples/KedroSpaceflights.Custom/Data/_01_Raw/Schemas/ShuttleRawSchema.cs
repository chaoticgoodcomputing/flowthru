using Flowthru.Abstractions;

namespace KedroSpaceflights.Custom.Data._01_Raw.Schemas;

/// <summary>
/// Raw shuttle data as read from Excel file.
/// Matches structure of Datasets/01_Raw/shuttles.xlsx
/// </summary>
[FlowthruSchema]
public partial record ShuttleRawSchema
{
  /// <summary>
  /// Shuttle identifier
  /// </summary>
  [SerializedLabel("id")]
  public required string Id { get; init; }

  /// <summary>
  /// Company identifier (foreign key to companies)
  /// </summary>
  [SerializedLabel("company_id")]
  public required string CompanyId { get; init; }

  /// <summary>
  /// Shuttle location/origin
  /// </summary>
  [SerializedLabel("shuttle_location")]
  public string? ShuttleLocation { get; init; }

  /// <summary>
  /// Shuttle type/model
  /// </summary>
  [SerializedLabel("shuttle_type")]
  public string? ShuttleType { get; init; }

  /// <summary>
  /// Engine type (e.g., Plasma, Quantum)
  /// </summary>
  [SerializedLabel("engine_type")]
  public string? EngineType { get; init; }

  /// <summary>
  /// Engine vendor/manufacturer
  /// </summary>
  [SerializedLabel("engine_vendor")]
  public string? EngineVendor { get; init; }

  /// <summary>
  /// Number of engines
  /// </summary>
  [SerializedLabel("engines")]
  public string? Engines { get; init; }

  /// <summary>
  /// Passenger capacity
  /// </summary>
  [SerializedLabel("passenger_capacity")]
  public string? PassengerCapacity { get; init; }

  /// <summary>
  /// Crew size
  /// </summary>
  [SerializedLabel("crew")]
  public string? Crew { get; init; }

  /// <summary>
  /// Cancellation policy (e.g., moderate, strict, flexible)
  /// </summary>
  [SerializedLabel("cancellation_policy")]
  public string? CancellationPolicy { get; init; }

  /// <summary>
  /// Price as currency string (e.g., "$1,234,567")
  /// </summary>
  [SerializedLabel("price")]
  public required string Price { get; init; }

  /// <summary>
  /// D-check completion status as "t" or "f"
  /// </summary>
  [SerializedLabel("d_check_complete")]
  public required string DCheckComplete { get; init; }

  /// <summary>
  /// Moon clearance completion status as "t" or "f"
  /// </summary>
  [SerializedLabel("moon_clearance_complete")]
  public required string MoonClearanceComplete { get; init; }
}
