using KedroSpaceflights.Custom.Data._03_Primary.Schemas;

namespace KedroSpaceflights.Custom.Pipelines.DataScience.Nodes;

/// <summary>
/// Splits model input data into training and testing sets.
/// Extracts features and target variable (price) for ML training.
/// </summary>
public static class CreateTestTrainSplitNode
{
  // Following FlowThru's artifact colocation policy:
  /// Parameters for data science pipeline model training.
  /// Configures train/test split and feature selection.
  /// </summary>
  public record TestTrainSplitParams
  {
    /// <summary>
    /// Proportion of data to use for testing (e.g., 0.2 for 20%)
    /// </summary>
    public double TestSize { get; init; } = 0.2;

    /// <summary>
    /// Random seed for reproducible splits
    /// </summary>
    public int RandomState { get; init; } = 3;
  }

  /// <summary>
  /// Creates a transformation function that splits data into train/test sets.
  /// </summary>
  /// <param name="parameters">Parameters controlling the split (test size, random seed)</param>
  public static Func<
    IEnumerable<ModelInputSchema>,
    Task<(
      IEnumerable<FeatureRow> XTrain,
      IEnumerable<FeatureRow> XTest,
      IEnumerable<TargetValue> YTrain,
      IEnumerable<TargetValue> YTest
    )>
  > Create(TestTrainSplitParams? parameters = null)
  {
    var config = parameters ?? new TestTrainSplitParams();

    return async (input) =>
    {
      var data = input.ToList();

      // Convert to feature rows and extract prices in a single pass
      var featureRowsAndPrices = data.Select(row =>
          (
            Features: new FeatureRow
            {
              Engines = (float)row.Engines,
              PassengerCapacity = (float)row.PassengerCapacity,
              Crew = (float)row.Crew,
              DCheckComplete = row.DCheckComplete,
              IataApproved = row.IataApproved,
              CompanyRating = (float)row.CompanyRating,
              ReviewScoresRating = (float)row.ReviewScoresRating,
              Price = (float)row.Price,
            },
            Price: row.Price
          )
        )
        .ToList();

      // Perform train/test split using Fisher-Yates shuffle
      var random = new Random(config.RandomState);
      var shuffled = featureRowsAndPrices.ToList();

      // In-place Fisher-Yates shuffle
      for (int i = shuffled.Count - 1; i > 0; i--)
      {
        int j = random.Next(i + 1);
        (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
      }

      var testCount = (int)(shuffled.Count * config.TestSize);
      var trainCount = shuffled.Count - testCount;

      var trainData = shuffled.Take(trainCount).ToList();
      var testData = shuffled.Skip(trainCount).ToList();

      // Create multi-output result as tuple (not wrapped in IEnumerable)
      var result = (
        XTrain: (IEnumerable<FeatureRow>)trainData.Select(x => x.Features).ToList(),
        XTest: (IEnumerable<FeatureRow>)testData.Select(x => x.Features).ToList(),
        YTrain: (IEnumerable<TargetValue>)
          trainData.Select(x => new TargetValue { Price = x.Price }).ToList(),
        YTest: (IEnumerable<TargetValue>)
          testData.Select(x => new TargetValue { Price = x.Price }).ToList()
      );

      return await Task.FromResult(result);
    };
  }
}
