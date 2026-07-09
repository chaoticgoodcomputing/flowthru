using Flowthru.Data.Schema;

namespace SpaceflightsDuckDB.Data._08_Reporting.Schemas;

/// <summary>
/// Represents a per-company summary aggregated from the model input table.
/// Produced by the engine-side aggregate transform in the Reporting Flow.
/// </summary>
/// <remarks>
/// The SQL result is verified against this Schema before the output file is
/// written: every property below must come back from the query with a matching
/// name and a compatible type. Aggregates often widen (DuckDB's <c>SUM</c> over
/// an integer column is a 128-bit integer), so the transform SQL uses explicit
/// <c>CAST</c>s to land on these declared types.
/// </remarks>
[FlowthruSchema]
public partial record CompanySummarySchema
{
  /// <summary>
  /// Identifier of the company being summarized.
  /// </summary>
  [SerializedLabel("company_id")]
  public required string CompanyId { get; init; }

  /// <summary>
  /// Number of rated shuttles the company operates.
  /// </summary>
  [SerializedLabel("shuttle_count")]
  public required long ShuttleCount { get; init; }

  /// <summary>
  /// Average trip price across the company's rated shuttles.
  /// </summary>
  [SerializedLabel("avg_price")]
  public required double AvgPrice { get; init; }

  /// <summary>
  /// Average review score across the company's rated shuttles.
  /// </summary>
  [SerializedLabel("avg_review_score")]
  public required double AvgReviewScore { get; init; }

  /// <summary>
  /// Total passenger capacity across the company's rated shuttles.
  /// </summary>
  [SerializedLabel("total_passenger_capacity")]
  public required long TotalPassengerCapacity { get; init; }
}
