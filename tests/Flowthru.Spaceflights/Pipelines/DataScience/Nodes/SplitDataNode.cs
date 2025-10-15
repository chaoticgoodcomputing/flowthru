using Flowthru.Nodes;
using Flowthru.Nodes.Attributes;
using Flowthru.Spaceflights.Data.Schemas.Processed;
using Flowthru.Spaceflights.Data.Schemas.Models;
using Flowthru.Spaceflights.Pipelines.DataScience.Parameters;

namespace Flowthru.Spaceflights.Pipelines.DataScience.Nodes;

/// <summary>
/// Splits model input data into training and testing sets.
/// Extracts features and target variable (price) for ML training.
/// </summary>
[Node("split_data", "Splits data into features and targets training and test sets")]
[NodeInput("model_input_table")]
[NodeOutput("train_test_split")]
public class SplitDataNode
    : Node<ModelInputSchema, TrainTestSplit>
    , IParameterizedNode<ModelInputSchema, TrainTestSplit, ModelOptions>
{
  public ModelOptions Parameters { get; set; } = new();

  protected override Task<IEnumerable<TrainTestSplit>> TransformInternal(
      IEnumerable<ModelInputSchema> input)
  {
    var data = input.ToList();

    // Convert to feature rows
    var featureRows = data.Select(row => new FeatureRow
    {
      Engines = (float)(row.Engines ?? 0),
      PassengerCapacity = (float)(row.PassengerCapacity ?? 0),
      Crew = (float)(row.Crew ?? 0),
      DCheckComplete = row.DCheckComplete,
      MoonClearanceComplete = row.MoonClearanceComplete,
      IataApproved = row.IataApproved,
      CompanyRating = (float)row.CompanyRating,
      ReviewScoresRating = (float)(row.ReviewScoresRating ?? 0),
      Price = (float)row.Price
    }).ToList();

    // Perform train/test split
    var random = new Random(Parameters.RandomState);
    var shuffled = featureRows.OrderBy(x => random.Next()).ToList();

    var testCount = (int)(shuffled.Count * Parameters.TestSize);
    var trainCount = shuffled.Count - testCount;

    var trainData = shuffled.Take(trainCount).ToList();
    var testData = shuffled.Skip(trainCount).ToList();

    // Extract features and targets
    var split = new TrainTestSplit
    {
      XTrain = trainData,
      XTest = testData,
      YTrain = trainData.Select(r => (decimal)r.Price).ToList(),
      YTest = testData.Select(r => (decimal)r.Price).ToList()
    };

    // Return as singleton collection
    return Task.FromResult(new[] { split }.AsEnumerable());
  }
}
