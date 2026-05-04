namespace SpaceflightsStagingSchema.Data._05_ModelInput.Schemas;

public record TrainingData
{
  public FeatureVector Features { get; init; } = null!;
  public decimal Label { get; init; }
}

public record TestData
{
  public FeatureVector Features { get; init; } = null!;
  public decimal Label { get; init; }
}

public record FeatureVector
{
  public int Engines { get; init; }
  public int PassengerCapacity { get; init; }
  public int Crew { get; init; }
  public bool DCheckComplete { get; init; }
  public bool MoonClearanceComplete { get; init; }
  public bool IataApproved { get; init; }
  public decimal CompanyRating { get; init; }
  public decimal ReviewScoresRating { get; init; }
}
