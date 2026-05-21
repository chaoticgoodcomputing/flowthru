using Flowthru.Step;
using Spaceflights.Data._03_Primary.Schemas;
using Spaceflights.Data._05_ModelInput.Schemas;

namespace Spaceflights.Flows.DataScience.Steps;

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
  /// Canonical Func-returning Create — the transform receives the
  /// model input rows and the configuration-bound <see cref="ModelOptions"/>
  /// as a tuple input. Options come from the catalog like any other
  /// fingerprintable input (Phase 5/8 of the smart-caching RFC); a
  /// change to <c>Flowthru:Flows:DataScience:ModelOptions</c> in
  /// <c>appsettings.json</c> invalidates this step's cached output
  /// automatically.
  /// </summary>
  public static Func<
    (IEnumerable<ModelInputTableSchema>, ModelOptions),
    (IEnumerable<TrainingData>, IEnumerable<TestData>)
  > Create() => input =>
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
  };
}
