using System.ComponentModel.DataAnnotations;
using Flowthru.Nodes;
using Flowthru.Tests.KedroSpaceflights.Data.Schemas.Models;
using Flowthru.Tests.KedroSpaceflights.Data.Schemas.Processed;

namespace Flowthru.Tests.KedroSpaceflights.Pipelines.DataScience.Nodes;

/// <summary>
/// Splits model input data into training and testing sets.
/// Extracts features and target variable (price) for ML training.
/// 
/// <para><strong>Colocation Pattern:</strong></para>
/// <para>
/// This node follows FlowThru's artifact colocation policy (similar to React Props pattern):
/// - Node class and its associated artifacts (parameters, output schemas) live in the same file
/// - Pure catalog entry schemas (domain models) remain in Data/Schemas/
/// - This keeps node-specific coordination types together with the node logic
/// </para>
/// 
/// <para><strong>Multi-output Pattern:</strong></para>
/// <para>
/// Produces multi-output via SplitDataOutputs schema. The pipeline uses
/// OutputMapping&lt;SplitDataOutputs&gt; to map each property to a separate catalog entry,
/// allowing downstream nodes to reference individual datasets independently.
/// </para>
/// 
/// <para><strong>Parameters Pattern:</strong></para>
/// <para>
/// Uses third type parameter (ModelParams) for parameters, which provides
/// the Parameters property via inheritance. Maintains parameterless constructor
/// for type reference instantiation (required for distributed/parallel execution).
/// </para>
/// </summary>
public class CreateTestTrainSplitNode
  : NodeBase<
      IEnumerable<ModelInputSchema>,
      (IEnumerable<FeatureRow> XTrain,
       IEnumerable<FeatureRow> XTest,
       IEnumerable<TargetValue> YTrain,
       IEnumerable<TargetValue> YTest),
      ModelParams> {
  // Parameters property inherited from NodeBase<TInput, TOutput, TParameters>
  // public ModelParams Parameters { get; set; } = new();

  protected override Task<(
    IEnumerable<FeatureRow> XTrain,
    IEnumerable<FeatureRow> XTest,
    IEnumerable<TargetValue> YTrain,
    IEnumerable<TargetValue> YTest
  )> Transform(IEnumerable<ModelInputSchema> input) {
    var data = input.ToList();

    // Convert to feature rows and extract prices in a single pass
    var featureRowsAndPrices = data.Select(row => (
      Features: new FeatureRow {
        Engines = (float)row.Engines,
        PassengerCapacity = (float)row.PassengerCapacity,
        Crew = (float)row.Crew,
        DCheckComplete = row.DCheckComplete,
        IataApproved = row.IataApproved,
        CompanyRating = (float)row.CompanyRating,
        ReviewScoresRating = (float)row.ReviewScoresRating,
        Price = (float)row.Price
      },
      Price: row.Price
    )).ToList();

    // Perform train/test split using Fisher-Yates shuffle
    var random = new Random(Parameters.RandomState);
    var shuffled = featureRowsAndPrices.ToList();

    // In-place Fisher-Yates shuffle
    for (int i = shuffled.Count - 1; i > 0; i--) {
      int j = random.Next(i + 1);
      (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
    }

    var testCount = (int)(shuffled.Count * Parameters.TestSize);
    var trainCount = shuffled.Count - testCount;

    var trainData = shuffled.Take(trainCount).ToList();
    var testData = shuffled.Skip(trainCount).ToList();

    // Create multi-output result as tuple (not wrapped in IEnumerable)
    var result = (
      XTrain: (IEnumerable<FeatureRow>)trainData.Select(x => x.Features).ToList(),
      XTest: (IEnumerable<FeatureRow>)testData.Select(x => x.Features).ToList(),
      YTrain: (IEnumerable<TargetValue>)trainData.Select(x => new TargetValue { Price = x.Price }).ToList(),
      YTest: (IEnumerable<TargetValue>)testData.Select(x => new TargetValue { Price = x.Price }).ToList()
    );

    return Task.FromResult(result);
  }
}

#region Node Artifacts (Colocated)

// Following FlowThru's artifact colocation policy:
/// Parameters for data science pipeline model training.
/// Configures train/test split and feature selection.
/// </summary>
public record ModelParams {
  /// <summary>
  /// Proportion of data to use for testing (e.g., 0.2 for 20%)
  /// </summary>
  public double TestSize { get; init; } = 0.2;

  /// <summary>
  /// Random seed for reproducible splits
  /// </summary>
  public int RandomState { get; init; } = 3;
}

#endregion
