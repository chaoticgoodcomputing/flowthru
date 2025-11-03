using Flowthru.Abstractions;

namespace KedroSpaceflights.Pure.Data._03_Primary.Schemas;

/// <summary>
/// Represents a unified model input table combining shuttle, company, and review data.
/// Produced by joining preprocessed shuttle and company data with review scores.
/// </summary>
public record ModelInputTableSchema : IFlatSchema, IBinarySerializable, IStructuredSerializable
{
  /// <summary>
  /// Unique identifier for the shuttle.
  /// </summary>
  [SerializedLabel("shuttle_id")]
  public string ShuttleId { get; init; } = null!;

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
  /// D-check completion status.
  /// </summary>
  [SerializedLabel("d_check_complete")]
  public bool DCheckComplete { get; init; }

  /// <summary>
  /// Moon clearance completion status.
  /// </summary>
  [SerializedLabel("moon_clearance_complete")]
  public bool MoonClearanceComplete { get; init; }

  /// <summary>
  /// Trip price (target variable for prediction).
  /// </summary>
  [SerializedLabel("price")]
  public decimal Price { get; init; }

  /// <summary>
  /// IATA approval status of the operating company.
  /// </summary>
  [SerializedLabel("iata_approved")]
  public bool IataApproved { get; init; }

  /// <summary>
  /// Rating of the operating company as a decimal ratio (0.0 to 1.0).
  /// </summary>
  [SerializedLabel("company_rating")]
  public decimal CompanyRating { get; init; }

  /// <summary>
  /// Review score rating for this shuttle.
  /// </summary>
  [SerializedLabel("review_scores_rating")]
  public decimal ReviewScoresRating { get; init; }
}
