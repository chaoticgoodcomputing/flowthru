using Flowthru.Core.Abstractions;

namespace KedroSpaceflightsSpark.Data._01_Raw.Schemas;

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
