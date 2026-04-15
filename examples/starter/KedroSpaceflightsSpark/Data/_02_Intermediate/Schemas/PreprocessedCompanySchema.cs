using Flowthru.Core.Abstractions;

namespace KedroSpaceflightsSpark.Data._02_Intermediate.Schemas;

/// <summary>
/// Preprocessed company data with strongly-typed fields.
/// Uses double for floating-point fields to match Spark's DoubleType columns.
/// </summary>
[FlowthruSchema]
public partial record PreprocessedCompanySchema
{
  [SerializedLabel("id")]
  public required string Id { get; init; }

  [SerializedLabel("company_rating")]
  public required double CompanyRating { get; init; }

  [SerializedLabel("iata_approved")]
  public required bool IataApproved { get; init; }

  [SerializedLabel("company_location")]
  public required string CompanyLocation { get; init; }
}
