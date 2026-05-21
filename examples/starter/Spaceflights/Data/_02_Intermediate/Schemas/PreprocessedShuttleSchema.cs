using Flowthru.Data.Schema;

namespace Spaceflights.Data._02_Intermediate.Schemas;

/// <summary>
/// Represents preprocessed shuttle data with strongly-typed fields.
/// Produced by parsing and validating raw shuttle data.
/// </summary>
/// <remarks>
/// Uses required members to enforce that all critical fields must be set
/// during construction, preventing accidental omission in pipeline nodes.
/// </remarks>
[FlowthruSchema]
public partial record PreprocessedShuttleSchema
{
  /// <summary>
  /// Unique identifier for the shuttle.
  /// </summary>
  [SerializedLabel("id")]
  public required string Id { get; init; }

  /// <summary>
  /// Type or model of the shuttle.
  /// </summary>
  [SerializedLabel("shuttle_type")]
  public required string ShuttleType { get; init; }

  /// <summary>
  /// Identifier of the company operating this shuttle.
  /// </summary>
  [SerializedLabel("company_id")]
  public required string CompanyId { get; init; }

  /// <summary>
  /// Number of engines.
  /// </summary>
  [SerializedLabel("engines")]
  public required int Engines { get; init; }

  /// <summary>
  /// Maximum passenger capacity.
  /// </summary>
  [SerializedLabel("passenger_capacity")]
  public required int PassengerCapacity { get; init; }

  /// <summary>
  /// Required crew size.
  /// </summary>
  [SerializedLabel("crew")]
  public required int Crew { get; init; }

  /// <summary>
  /// Trip price.
  /// </summary>
  [SerializedLabel("price")]
  public required decimal Price { get; init; }

  /// <summary>
  /// D-check completion status. Uses <see cref="CheckStatus"/> so the on-disk "t"/"f" flag
  /// is round-tripped through Flowthru's <c>[SerializedEnum]</c> infrastructure rather than
  /// hand-coded into a <c>bool</c>.
  /// </summary>
  [SerializedLabel("d_check_complete")]
  public required CheckStatus DCheckComplete { get; init; }

  /// <summary>
  /// Moon clearance completion status. Same <see cref="CheckStatus"/> pattern as
  /// <see cref="DCheckComplete"/>.
  /// </summary>
  [SerializedLabel("moon_clearance_complete")]
  public required CheckStatus MoonClearanceComplete { get; init; }
}
