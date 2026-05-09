using Flowthru.Data.Schema;

namespace SpaceflightsNewTypes.Data._01_Raw.Schemas;

/// <summary>
/// Represents raw company data as imported from text files.
/// All fields are stored as strings pending parsing.
/// </summary>
/// <remarks>
/// This schema is the <em>declaration site</em> for <c>CompanyId</c>. The
/// <see cref="FlowthruColumnAttribute"/> tells the source generator to spin up a
/// <c>CompanyId</c> NewType wrapping <see cref="string"/> in this schema's namespace —
/// downstream layers reference it via
/// <c>using SpaceflightsNewTypes.Data._01_Raw.Schemas;</c> without needing to re-declare
/// the column.
/// </remarks>
[FlowthruSchema]
public partial record CompanySchema
{
  /// <summary>
  /// Unique identifier for the company. Distinct from <c>ShuttleId</c> at compile time —
  /// the compiler refuses to mix them in joins or projections.
  /// </summary>
  [FlowthruColumn(typeof(string))]
  [SerializedLabel("id")]
  public required CompanyId Id { get; init; }

  /// <summary>
  /// Company rating as a percentage string (e.g., "90%").
  /// </summary>
  [SerializedLabel("company_rating")]
  public string CompanyRating { get; init; } = null!;

  /// <summary>
  /// IATA approval status as a string flag ("t" for true, "f" for false).
  /// </summary>
  [SerializedLabel("iata_approved")]
  public string IataApproved { get; init; } = null!;

  /// <summary>
  /// Geographic location of the company.
  /// </summary>
  [SerializedLabel("company_location")]
  public string CompanyLocation { get; init; } = null!;
}
