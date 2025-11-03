using Flowthru.Abstractions;

namespace KedroSpaceflights.Pure.Data._02_Intermediate.Schemas;

/// <summary>
/// Represents preprocessed company data with strongly-typed fields.
/// Produced by parsing and validating raw company data.
/// </summary>
public record PreprocessedCompanySchema : IFlatSchema, IBinarySerializable, IStructuredSerializable
{
  /// <summary>
  /// Unique identifier for the company.
  /// </summary>
  [SerializedLabel("id")]
  public string Id { get; init; } = null!;

  /// <summary>
  /// Company rating as a decimal ratio (0.0 to 1.0).
  /// </summary>
  [SerializedLabel("company_rating")]
  public decimal CompanyRating { get; init; }

  /// <summary>
  /// IATA approval status.
  /// </summary>
  [SerializedLabel("iata_approved")]
  public bool IataApproved { get; init; }

  /// <summary>
  /// Geographic location of the company.
  /// </summary>
  [SerializedLabel("company_location")]
  public string CompanyLocation { get; init; } = null!;
}
