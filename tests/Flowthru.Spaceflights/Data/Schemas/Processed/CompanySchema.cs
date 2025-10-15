using System.ComponentModel.DataAnnotations;

namespace Flowthru.Spaceflights.Data.Schemas.Processed;

/// <summary>
/// Processed company data with type conversions applied.
/// Output of PreprocessCompaniesNode.
/// </summary>
public record CompanySchema
{
  /// <summary>
  /// Company identifier
  /// </summary>
  [Required]
  public string Id { get; init; } = null!;

  /// <summary>
  /// Company rating as decimal (0.0 to 1.0)
  /// </summary>
  public decimal CompanyRating { get; init; }

  /// <summary>
  /// Company location/country
  /// </summary>
  public string? CompanyLocation { get; init; }

  /// <summary>
  /// Total fleet count
  /// </summary>
  public decimal? TotalFleetCount { get; init; }

  /// <summary>
  /// IATA approval status
  /// </summary>
  public bool IataApproved { get; init; }
}
