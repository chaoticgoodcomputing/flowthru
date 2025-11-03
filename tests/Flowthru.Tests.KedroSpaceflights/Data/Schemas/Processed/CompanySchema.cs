using System.ComponentModel.DataAnnotations;
using Flowthru.Abstractions;

namespace Flowthru.Tests.KedroSpaceflights.Data.Schemas.Processed;

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
  [Required]
  public string Id { get; init; } = null!;

  /// <summary>
  /// Company rating as decimal (0.0 to 1.0)
  /// </summary>
  [SerializedLabel("company_rating")]
  public decimal CompanyRating { get; init; }

  /// <summary>
  /// Company location/country
  /// </summary>
  [SerializedLabel("company_location")]
  public string CompanyLocation { get; init; } = null!;

  /// <summary>
  /// Total fleet count
  /// </summary>
  [SerializedLabel("total_fleet_count")]
  public decimal TotalFleetCount { get; init; }

  /// <summary>
  /// IATA approval status
  /// </summary>
  [SerializedLabel("iata_approved")]
  public bool IataApproved { get; init; }
}
