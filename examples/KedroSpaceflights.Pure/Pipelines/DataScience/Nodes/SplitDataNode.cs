using KedroSpaceflights.Pure.Data._03_Primary.Schemas;

namespace KedroSpaceflights.Pure.Pipelines.DataScience.Nodes;

public static class SplitDataNode
{
  public record ModelOptions
  {
    public double TestSize { get; init; } = 0.2;
    public int RandomState { get; init; } = 3;
    public string[] Features { get; init; } = Array.Empty<string>();
  }

  public static Func<
    IEnumerable<ModelInputTableSchema>,
    Task<(IEnumerable<TrainingData>, IEnumerable<TestData>)>
  > Create(ModelOptions options)
  {
    return async (input) =>
    {
      var data = input.ToList();

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

      return await Task.FromResult((trainData, testData));
    };
  }
}
