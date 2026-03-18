using Flowthru.Abstractions;

namespace SpaceflightsPythonEFCore.Data._05_ModelInput.Schemas;

/// <summary>
/// Feature vector for model training/testing. Produced by Python split_data node.
/// </summary>
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
