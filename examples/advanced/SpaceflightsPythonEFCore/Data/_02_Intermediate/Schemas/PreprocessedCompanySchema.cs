using Flowthru.Data.Schema;

namespace SpaceflightsPythonEFCore.Data._02_Intermediate.Schemas;

/// <summary>
/// Preprocessed company data with strongly-typed fields, produced by the C# DataProcessing pipeline.
/// </summary>
[FlowthruSchema]
public partial record PreprocessedCompanySchema
{
  [SerializedLabel("id")]
  public required int Id { get; init; }

  [SerializedLabel("company_rating")]
  public required double CompanyRating { get; init; }

  [SerializedLabel("iata_approved")]
  public required bool IataApproved { get; init; }

  [SerializedLabel("company_location")]
  public required string CompanyLocation { get; init; }

  /// <summary>
  /// Total fleet count (nullable if data was missing or unparseable).
  /// </summary>
  [SerializedLabel("total_fleet_count")]
  public double? TotalFleetCount { get; init; }
}
