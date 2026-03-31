using SpaceflightsDistributed.DataProcessing.Data._03_Primary.Schemas;
using SpaceflightsDistributed.DataScience.Data._05_ModelInput.Schemas;

namespace SpaceflightsDistributed.DataScience.Pipelines.DataScience.Nodes;

public static class SplitDataNode
{
  public record ModelOptions
  {
    public double TestSize { get; init; } = 0.2;
    public int RandomState { get; init; } = 3;
  }

  public static Func<
    IEnumerable<ModelInputTableSchema>,
    (IEnumerable<TrainingData>, IEnumerable<TestData>)
  > Create(ModelOptions options)
  {
    return (input) =>
    {
      var data = input.ToList();
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
