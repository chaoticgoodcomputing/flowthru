using Flowthru.Core.Steps;
using Flowthru.Misc.DataFrames;
using KedroSpaceflightsSpark.Data._03_Primary.Schemas;
using KedroSpaceflightsSpark.Data._05_ModelInput.Schemas;

namespace KedroSpaceflightsSpark.Flows.DataScience.Steps;

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
    TypedFrame<ModelInputTableSchema>,
    (IEnumerable<TrainingData>, IEnumerable<TestData>)
  > Create(ModelOptions options)
  {
    return (input) =>
    {
      // Filter out rows with non-positive prices in Spark before materializing.
      // This is the last point where Spark operations can be applied — the shuffle
      // below requires the full dataset in memory.
      var data = input.Where(r => r.Price > 0).ToList();

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
}
