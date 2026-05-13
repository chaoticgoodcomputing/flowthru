using Flowthru.Step;
using SpaceflightsHybridCatalog.Data._03_Primary.Schemas;
using SpaceflightsHybridCatalog.Data._05_ModelInput.Schemas;

namespace SpaceflightsHybridCatalog.Flows.DataScience.Steps;

/// <summary>
/// Splits model input data into training and test sets for model evaluation.
/// </summary>
[FlowthruStep]
public static class SplitDataStep
{
  public record ModelOptions
  {
    public double TestSize { get; init; } = 0.2;
    public int RandomState { get; init; } = 3;
    public string[] Features { get; init; } = Array.Empty<string>();
  }

  public static Func<
    IEnumerable<ModelInputTableSchema>,
    (IEnumerable<TrainingData>, IEnumerable<TestData>)
  > Create(ModelOptions options) => rawData =>
  {
    var data = rawData.ToList();

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
      })
      .ToList();

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
      })
      .ToList();

    return (trainData, testData);
  };
}
