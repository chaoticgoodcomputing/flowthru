using Flowthru.Abstractions;

namespace KedroSpaceflights.Pure.Data._01_Raw.Schemas;

public record CompanySchema : IFlatSchema, ITextSerializable
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
