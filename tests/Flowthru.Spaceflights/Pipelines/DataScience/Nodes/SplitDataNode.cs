using Flowthru.Nodes;
using Flowthru.Spaceflights.Data.Schemas.Processed;
using Flowthru.Spaceflights.Data.Schemas.Models;
using Flowthru.Spaceflights.Pipelines.DataScience.Parameters;

namespace Flowthru.Spaceflights.Pipelines.DataScience.Nodes;

/// <summary>
/// Splits model input data into training and testing sets.
/// Extracts features and target variable (price) for ML training.
/// 
/// Produces multi-output via SplitDataOutputs schema. The pipeline uses
/// OutputMapping&lt;SplitDataOutputs&gt; to map each property to a separate catalog entry,
/// allowing downstream nodes to reference individual datasets independently.
/// 
/// Uses third type parameter (ModelOptions) for parameters, which provides
/// the Parameters property via inheritance. Maintains parameterless constructor
/// for type reference instantiation (required for distributed/parallel execution).
/// </summary>
public class SplitDataNode : Node<ModelInputSchema, SplitDataOutputs, ModelOptions>
{
  // Parameters property inherited from Node<TInput, TOutput, TParameters>
  // public ModelOptions Parameters { get; set; } = new();

  protected override Task<IEnumerable<SplitDataOutputs>> Transform(
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

    // Create multi-output result
    // Framework will unpack this into separate catalog entries based on [CatalogOutput] attributes
    var outputs = new SplitDataOutputs
    {
      XTrain = trainData,
      XTest = testData,
      YTrain = trainData.Select(r => (decimal)r.Price).ToList(),
      YTest = testData.Select(r => (decimal)r.Price).ToList()
    };

    // Return as singleton collection
    return Task.FromResult(new[] { outputs }.AsEnumerable());
  }
}
