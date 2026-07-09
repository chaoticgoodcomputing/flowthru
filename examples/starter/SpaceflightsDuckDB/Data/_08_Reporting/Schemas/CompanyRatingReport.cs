using Flowthru.Data.Schema;

namespace SpaceflightsDuckDB.Data._08_Reporting.Schemas;

/// <summary>
/// Represents one entry in the top-rated-companies report.
/// </summary>
/// <remarks>
/// Uses required members to enforce that all critical report fields must be set
/// during construction, ensuring complete reporting outputs.
/// </remarks>
[FlowthruSchema]
public partial record CompanyRatingReport
{
  /// <summary>
  /// Rank of the company by average review score (1 = highest).
  /// </summary>
  [SerializedLabel("rank")]
  public required int Rank { get; init; }

  /// <summary>
  /// Identifier of the company.
  /// </summary>
  [SerializedLabel("company_id")]
  public required string CompanyId { get; init; }

  /// <summary>
  /// Number of rated shuttles the company operates.
  /// </summary>
  [SerializedLabel("shuttle_count")]
  public required long ShuttleCount { get; init; }

  /// <summary>
  /// Average review score across the company's rated shuttles.
  /// </summary>
  [SerializedLabel("avg_review_score")]
  public required double AvgReviewScore { get; init; }

  /// <summary>
  /// Average trip price across the company's rated shuttles.
  /// </summary>
  [SerializedLabel("avg_price")]
  public required double AvgPrice { get; init; }
}
