using Flowthru.Abstractions;

namespace KedroSpaceflights.Pure.Data._02_Intermediate.Schemas;

public record PreprocessedCompanySchema : IFlatSchema, IBinarySerializable, IStructuredSerializable
{
  [SerializedLabel("id")]
  public string Id { get; init; } = null!;

  [SerializedLabel("company_rating")]
  public decimal CompanyRating { get; init; }

  [SerializedLabel("iata_approved")]
  public bool IataApproved { get; init; }

  [SerializedLabel("company_location")]
  public string CompanyLocation { get; init; } = null!;
}
