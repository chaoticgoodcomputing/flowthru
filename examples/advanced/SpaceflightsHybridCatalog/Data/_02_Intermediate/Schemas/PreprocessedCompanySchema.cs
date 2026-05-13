using Flowthru.Data.Schema;

namespace SpaceflightsHybridCatalog.Data._02_Intermediate.Schemas;

/// <summary>
/// Represents preprocessed company data with strongly-typed fields.
/// Produced by parsing and validating raw company data.
/// </summary>
[FlowthruSchema]
public partial record PreprocessedCompanySchema
{
  [SerializedLabel("id")]
  public required string Id { get; init; }

  [SerializedLabel("company_rating")]
  public required decimal CompanyRating { get; init; }

  [SerializedLabel("iata_approved")]
  public required bool IataApproved { get; init; }

  [SerializedLabel("company_location")]
  public required string CompanyLocation { get; init; }
}
