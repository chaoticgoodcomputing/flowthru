namespace Flowthru.Spaceflights.Pipelines.DataScience.Parameters;

/// <summary>
/// Parameters for data science pipeline model training.
/// Configures train/test split and feature selection.
/// </summary>
public record ModelOptions
{
  /// <summary>
  /// Proportion of data to use for testing (e.g., 0.2 for 20%)
  /// </summary>
  public double TestSize { get; init; } = 0.2;

  /// <summary>
  /// Random seed for reproducible splits
  /// </summary>
  public int RandomState { get; init; } = 3;

  /// <summary>
  /// Feature columns to use for model training.
  /// Should match properties on ModelInputSchema.
  /// </summary>
  public List<string> Features { get; init; } = new()
    {
        "Engines",
        "PassengerCapacity",
        "Crew",
        "DCheckComplete",
        "MoonClearanceComplete",
        "IataApproved",
        "CompanyRating",
        "ReviewScoresRating"
    };
}
