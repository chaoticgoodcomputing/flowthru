using Flowthru.Core.Abstractions;

namespace KedroSpaceflightsPython.Data._05_ModelInput.Schemas;

/// <summary>
/// Represents preprocessed company data with strongly-typed fields.
/// Produced by parsing and validating raw company data.
/// </summary>
/// <remarks>
/// Uses required members to enforce that all critical fields must be set
/// during construction, preventing accidental omission in pipeline nodes.
/// </remarks>
[FlowthruSchema]
public partial record XValues
{
  [SerializedLabel("engines")]
  public int Engines { get; init; }

  [SerializedLabel("passenger_capacity")]
  public int PassengerCapacity { get; init; }

  [SerializedLabel("crew")]
  public int Crew { get; init; }

  [SerializedLabel("d_check_complete")]
  public bool DCheckComplete { get; init; }

  [SerializedLabel("moon_clearance_complete")]
  public bool MoonClearanceComplete { get; init; }

  [SerializedLabel("iata_approved")]
  public bool IataApproved { get; init; }

  [SerializedLabel("company_rating")]
  public double CompanyRating { get; init; }

  [SerializedLabel("review_scores_rating")]
  public double ReviewScoresRating { get; init; }
}
