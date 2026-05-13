using Flowthru.Data.Schema;

namespace SpaceflightsHybridCatalog.Data._01_Raw.Schemas;

/// <summary>
/// Represents raw company data as imported from text files.
/// All fields are stored as strings pending parsing.
/// </summary>
[FlowthruSchema]
public partial record CompanySchema
{
  [SerializedLabel("id")]
  public string Id { get; init; } = null!;

  [SerializedLabel("company_rating")]
  public string CompanyRating { get; init; } = null!;

  [SerializedLabel("iata_approved")]
  public string IataApproved { get; init; } = null!;

  [SerializedLabel("company_location")]
  public string CompanyLocation { get; init; } = null!;
}
