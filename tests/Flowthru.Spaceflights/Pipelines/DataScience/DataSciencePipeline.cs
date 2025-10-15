using Flowthru.Pipelines;
using Flowthru.Spaceflights.Data;
using Flowthru.Spaceflights.Pipelines.DataScience.Nodes;

namespace Flowthru.Spaceflights.Pipelines.DataScience;

/// <summary>
/// Data science pipeline that splits data, trains model, and evaluates performance.
/// 
/// <para><strong>Compile-Time Type Safety:</strong></para>
/// <para>
/// This pipeline uses a strongly-typed catalog (SpaceflightsCatalog) to ensure:
/// - All catalog references are validated at compile-time
/// - Node input/output types must match catalog entry types
/// - OutputMapping enforces property-to-catalog type consistency
/// - Refactoring tools work seamlessly (rename, find references)
/// - IntelliSense shows available catalog entries with their types
/// </para>
/// 
/// <para><strong>Zero Runtime Type Errors:</strong></para>
/// <para>
/// If this code compiles, the pipeline is correctly wired. Type mismatches
/// between nodes and catalog entries will cause compilation failures, not runtime errors.
/// </para>
/// </summary>
public static class DataSciencePipeline
{
  public static Pipeline Create(SpaceflightsCatalog catalog, ModelOptions? options = null)
  {
    return PipelineBuilder.CreatePipeline(pipeline =>
    {
      // Node 1: Split data into train/test sets (multi-output with compile-time type checking)
      // OutputMapping<T>.Add<TProp> ensures each property type matches its catalog entry type
      var splitOutputs = new OutputMapping<SplitDataOutputs>();
      splitOutputs.Add(s => s.XTrain, catalog.XTrain);  // ✅ Both IEnumerable<FeatureRow>
      splitOutputs.Add(s => s.XTest, catalog.XTest);    // ✅ Both IEnumerable<FeatureRow>
      splitOutputs.Add(s => s.YTrain, catalog.YTrain);  // ✅ Both IEnumerable<decimal>
      splitOutputs.Add(s => s.YTest, catalog.YTest);    // ✅ Both IEnumerable<decimal>

      pipeline.AddNode<SplitDataNode>(
        input: catalog.ModelInputTable,  // ✅ Type-checked: ICatalogEntry<ModelInputSchema>
        outputMapping: splitOutputs,
        name: "split_data_node",
        configureNode: node => node.Parameters = options ?? new ModelOptions()
      );

      // Node 2: Train regression model (multi-input with compile-time type checking)
      pipeline.AddNode<TrainModelNode>(
        inputs: (catalog.XTrain, catalog.YTrain),  // ✅ Tuple type-checked against node signature
        output: catalog.Regressor,                  // ✅ Type-checked: ICatalogEntry<ITransformer>
        name: "train_model_node"
      );

      // Node 3: Evaluate model (three inputs with compile-time type checking)
      pipeline.AddNode<EvaluateModelNode>(
        inputs: (catalog.Regressor, catalog.XTest, catalog.YTest),  // ✅ All types validated
        output: catalog.ModelMetrics,                                // ✅ Type-checked: ICatalogEntry<ModelMetrics>
        name: "evaluate_model_node"
      // Optional: configureNode: node => node.Logger = logger
      );
    });
  }
}
