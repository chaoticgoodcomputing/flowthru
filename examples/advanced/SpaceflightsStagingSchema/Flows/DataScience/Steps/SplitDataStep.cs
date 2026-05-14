using Flowthru.Step;
using SpaceflightsStagingSchema.Data._03_Primary.Schemas;
using SpaceflightsStagingSchema.Data._05_ModelInput.Schemas;

namespace SpaceflightsStagingSchema.Flows.DataScience.Steps;

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
    (IEnumerable<ModelInputTableSchema>, ModelOptions),
    (IEnumerable<TrainingData>, IEnumerable<TestData>)
  > Create() => input =>
  {
    var (rawData, options) = input;
    var data = rawData.ToList();

    var random = new Random(options.RandomState);
    var shuffled = data.OrderBy(_ => random.Next()).ToList();

    var splitIndex = (int)(shuffled.Count * (1 - options.TestSize));

    var trainData = shuffled
      .Take(splitIndex)
      .Select(row => new TrainingData
      {
        Features = ToFeatureVector(row),
        Label = row.Price,
      });

    var testData = shuffled
      .Skip(splitIndex)
      .Select(row => new TestData
      {
        Features = ToFeatureVector(row),
        Label = row.Price,
      });

    return (trainData, testData);
  };

  private static FeatureVector ToFeatureVector(ModelInputTableSchema row) =>
    new FeatureVector
    {
      Engines = row.Engines,
      PassengerCapacity = row.PassengerCapacity,
      Crew = row.Crew,
      DCheckComplete = row.DCheckComplete,
      MoonClearanceComplete = row.MoonClearanceComplete,
      IataApproved = row.IataApproved,
      CompanyRating = row.CompanyRating,
      ReviewScoresRating = row.ReviewScoresRating,
    };
}
