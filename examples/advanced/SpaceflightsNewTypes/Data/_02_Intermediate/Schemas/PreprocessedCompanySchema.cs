using Flowthru.Data.Schema;
using SpaceflightsNewTypes.Data._01_Raw.Schemas;

namespace SpaceflightsNewTypes.Data._02_Intermediate.Schemas;

/// <summary>
/// Represents preprocessed company data with strongly-typed fields.
/// Produced by parsing and validating raw company data.
/// </summary>
/// <remarks>
/// Uses required members to enforce that all critical fields must be set
/// during construction, preventing accidental omission in pipeline nodes.
/// </remarks>
[FlowthruSchema]
public partial record PreprocessedCompanySchema
{
  /// <summary>
  /// Unique identifier for the company. References <see cref="CompanyId"/> declared in
  /// <see cref="CompanySchema"/> — no <c>[FlowthruColumn]</c> needed here.
  /// </summary>
  [SerializedLabel("id")]
  public required CompanyId Id { get; init; }

  /// <summary>
  /// Company rating as a decimal ratio (0.0 to 1.0).
  /// </summary>
  [SerializedLabel("company_rating")]
  public required decimal CompanyRating { get; init; }

  /// <summary>
  /// IATA approval status.
  /// </summary>
  [SerializedLabel("iata_approved")]
  public required bool IataApproved { get; init; }

  /// <summary>
  /// Geographic location of the company.
  /// </summary>
  [SerializedLabel("company_location")]
  public required string CompanyLocation { get; init; }
}
