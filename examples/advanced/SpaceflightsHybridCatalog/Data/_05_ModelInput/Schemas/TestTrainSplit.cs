using SpaceflightsHybridCatalog.Data._02_Intermediate.Schemas;

namespace SpaceflightsHybridCatalog.Data._05_ModelInput.Schemas;

/// <summary>Training data pair (features + label).</summary>
public record TrainingData
{
  public FeatureVector Features { get; init; } = null!;
  public decimal Label { get; init; }
}

/// <summary>Test data pair (features + label).</summary>
public record TestData
{
  public FeatureVector Features { get; init; } = null!;
  public decimal Label { get; init; }
}

/// <summary>Feature vector for model training/testing.</summary>
public record FeatureVector
{
  public int Engines { get; init; }
  public int PassengerCapacity { get; init; }
  public int Crew { get; init; }
  public CheckStatus DCheckComplete { get; init; }
  public CheckStatus MoonClearanceComplete { get; init; }
  public bool IataApproved { get; init; }
  public decimal CompanyRating { get; init; }
  public decimal ReviewScoresRating { get; init; }
}
