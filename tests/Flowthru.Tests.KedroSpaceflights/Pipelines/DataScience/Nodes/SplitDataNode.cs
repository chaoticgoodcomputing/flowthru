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
public class SplitDataNode : NodeBase<ModelInputSchema, SplitDataOutputs, ModelParams> {
  // Parameters property inherited from NodeBase<TInput, TOutput, TParameters>
  // public ModelParams Parameters { get; set; } = new();

  protected override Task<IEnumerable<SplitDataOutputs>> Transform(
      IEnumerable<ModelInputSchema> input) {
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

    // Create multi-output result
    // Framework will unpack this into separate catalog entries based on [CatalogOutput] attributes
    var outputs = new SplitDataOutputs {
      XTrain = trainData.Select(x => x.Features).ToList(),
      XTest = testData.Select(x => x.Features).ToList(),
      YTrain = trainData.Select(x => x.Price).ToList(),
      YTest = testData.Select(x => x.Price).ToList()
    };

    // Return as singleton collection
    return Task.FromResult(new[] { outputs }.AsEnumerable());
  }
}

#region Node Artifacts (Colocated)

// Following FlowThru's artifact colocation policy:
// Node-specific types (parameters, output schemas) are defined with the node that uses them.
// This mirrors the React Props pattern where component-specific types live with the component.
// Pure domain schemas (catalog entry types) remain in Data/Schemas/.

/// <summary>
/// Multi-output schema for train/test split operation.
/// Pure data schema with no catalog coupling.
/// 
/// <para>
/// Properties will be mapped to catalog entries at pipeline registration time
/// using OutputMapping&lt;T&gt; to maintain separation of concerns:
/// </para>
/// <list type="bullet">
/// <item>Schema layer: Pure data shape definitions</item>
/// <item>Catalog layer: Data storage/naming bindings</item>
/// </list>
/// </summary>
public record SplitDataOutputs {
  /// <summary>
  /// Training features
  /// </summary>
  public IEnumerable<FeatureRow> XTrain { get; init; } = null!;

  /// <summary>
  /// Testing features
  /// </summary>
  public IEnumerable<FeatureRow> XTest { get; init; } = null!;

  /// <summary>
  /// Training targets (prices)
  /// </summary>
  public IEnumerable<decimal> YTrain { get; init; } = null!;

  /// <summary>
  /// Testing targets (prices)
  /// </summary>
  public IEnumerable<decimal> YTest { get; init; } = null!;
}

/// <summary>
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
