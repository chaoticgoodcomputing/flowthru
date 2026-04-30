using Flowthru.Core.Steps;
using Flowthru.FUnit;
using KedroSpaceflightsFUnit.Data._03_Primary.Schemas;
using KedroSpaceflightsFUnit.Data._05_ModelInput.Schemas;

namespace KedroSpaceflightsFUnit.Flows.DataScience.Steps;

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

#if FUNIT_ENABLED
  /// <summary>FUnit tests for <see cref="SplitDataStep"/>.</summary>
  public class Tests : FUnitContext
  {
    private static ModelInputTableSchema Row(string id) =>
      new()
      {
        ShuttleId = id,
        ShuttleType = "Type A",
        CompanyId = "C1",
        Engines = 4,
        PassengerCapacity = 100,
        Crew = 8,
        DCheckComplete = true,
        MoonClearanceComplete = false,
        Price = 1000m,
        IataApproved = true,
        CompanyRating = 0.9m,
        ReviewScoresRating = 80m,
      };

    private static readonly ModelOptions DefaultOptions = new() { TestSize = 0.2, RandomState = 3 };

    /// <summary>
    /// With 10 inputs and a 20% test split, train should have 8 rows and test 2.
    /// </summary>
    [StepTest(typeof(SplitDataStep))]
    public void TenRows_SplitsCorrectly()
    {
      // Arrange
      var input = Samples.Generate(10, i => Row($"S{i}"));

      // Apply
      var (train, test) = Invoke(Create, (input, DefaultOptions));

      // Assert
      Assert.That(train.Count(), Is.EqualTo(8));
      Assert.That(test.Count(), Is.EqualTo(2));
    }

    /// <summary>
    /// Train count + test count must equal the total input count.
    /// </summary>
    [StepTest(typeof(SplitDataStep))]
    public void TrainPlusTest_EqualsTotal()
    {
      // Arrange
      var input = Samples.Generate(15, i => Row($"S{i}"));

      // Apply
      var (train, test) = Invoke(Create, (input, DefaultOptions));

      // Assert
      Assert.That(train.Count() + test.Count(), Is.EqualTo(15));
    }

    /// <summary>
    /// Same seed with same data must produce identical train/test order each call.
    /// </summary>
    [StepTest(typeof(SplitDataStep))]
    public void SameSeed_IsDeterministic()
    {
      // Arrange
      var input = Samples.Generate(10, i => Row($"S{i}"));

      // Apply
      var (train1, _) = Invoke(Create, (input, DefaultOptions));
      var (train2, _) = Invoke(Create, (input, DefaultOptions));

      // Assert
      var ids1 = train1.Select(r => r.Features.Engines).ToList();
      var ids2 = train2.Select(r => r.Features.Engines).ToList();
      Assert.That(ids1, Is.EqualTo(ids2));
    }
  }
#endif
}
