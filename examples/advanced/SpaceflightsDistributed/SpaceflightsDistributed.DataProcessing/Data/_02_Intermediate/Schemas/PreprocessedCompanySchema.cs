using Flowthru.Core.Abstractions;

namespace SpaceflightsDistributed.DataProcessing.Data._02_Intermediate.Schemas;

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
