using Flowthru.Core.Steps;
using SpaceflightsNewTypes.Data._03_Primary.Schemas;
using SpaceflightsNewTypes.Data._05_ModelInput.Schemas;

namespace SpaceflightsNewTypes.Flows.DataScience.Steps;

/// <summary>
/// Splits model input data into training and test sets for model evaluation.
/// </summary>
[FlowthruStep]
public static class SplitDataStep
{
  /// <summary>
  /// Configuration options for data splitting.
  /// </summary>
  public record ModelOptions
  {
    /// <summary>
    /// The proportion of data to use for testing. Default is 0.2 (20%).
    /// </summary>
    public double TestSize { get; init; } = 0.2;

    /// <summary>
    /// Random seed for reproducible shuffling. Default is 3.
    /// </summary>
    public int RandomState { get; init; } = 3;

    /// <summary>
    /// Feature names to include in the model (currently unused).
    /// </summary>
    public string[] Features { get; init; } = Array.Empty<string>();
  }

  /// <summary>
  /// Splits input data into training and test sets.
  /// </summary>
  public static (IEnumerable<TrainingData>, IEnumerable<TestData>) Create(
    (IEnumerable<ModelInputTableSchema> Data, ModelOptions Options) input
  )
  {
    var (rawData, options) = input;
    var data = rawData.ToList();

    // Use random state for reproducibility
    var random = new Random(options.RandomState);
    var shuffled = data.OrderBy(_ => random.Next()).ToList();

    var splitIndex = (int)(shuffled.Count * (1 - options.TestSize));

    var trainData = shuffled
      .Take(splitIndex)
      .Select(row => new TrainingData
      {
        Features = new FeatureVector
        {
          Engines = row.Engines,
          PassengerCapacity = row.PassengerCapacity,
          Crew = row.Crew,
          DCheckComplete = row.DCheckComplete,
          MoonClearanceComplete = row.MoonClearanceComplete,
          IataApproved = row.IataApproved,
          CompanyRating = row.CompanyRating,
          ReviewScoresRating = row.ReviewScoresRating,
        },
        Label = row.Price,
      });

    var testData = shuffled
      .Skip(splitIndex)
      .Select(row => new TestData
      {
        Features = new FeatureVector
        {
          Engines = row.Engines,
          PassengerCapacity = row.PassengerCapacity,
          Crew = row.Crew,
          DCheckComplete = row.DCheckComplete,
          MoonClearanceComplete = row.MoonClearanceComplete,
          IataApproved = row.IataApproved,
          CompanyRating = row.CompanyRating,
          ReviewScoresRating = row.ReviewScoresRating,
        },
        Label = row.Price,
      });

    return (trainData, testData);
  }
}
