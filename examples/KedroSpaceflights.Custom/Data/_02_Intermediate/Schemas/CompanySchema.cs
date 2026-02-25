using Flowthru.Abstractions;

namespace KedroSpaceflights.Custom.Data._02_Intermediate.Schemas;

/// <summary>
/// Processed company data with type conversions applied.
/// Output of PreprocessCompaniesNode.
/// </summary>
public record CompanySchema
  : IFlatSchema,
    ITextSerializable,
    IBinarySerializable,
    IStructuredSerializable
{
  /// <summary>
  /// Company identifier
  /// </summary>
  public required string Id { get; init; }

  /// <summary>
  /// Company rating as decimal (0.0 to 1.0)
  /// </summary>
  [SerializedLabel("company_rating")]
  public required decimal CompanyRating { get; init; }

  /// <summary>
  /// Company location/country
  /// </summary>
  [SerializedLabel("company_location")]
  public required string CompanyLocation { get; init; }

  /// <summary>
  /// Total fleet count
  /// </summary>
  [SerializedLabel("total_fleet_count")]
  public required decimal TotalFleetCount { get; init; }

  /// <summary>
  /// IATA approval status
  /// </summary>
  [SerializedLabel("iata_approved")]
  public required bool IataApproved { get; init; }
}
