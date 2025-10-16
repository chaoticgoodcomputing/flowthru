using Microsoft.ML;
using Flowthru.Nodes;
using Flowthru.Pipelines;
using Flowthru.Pipelines.Mapping;
using Flowthru.Spaceflights.Data;
using Flowthru.Spaceflights.Data.Schemas.Models;
using Flowthru.Spaceflights.Data.Schemas.Processed;
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
/// - CatalogMap enforces property-to-catalog type consistency
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
      // Node 1: Split data into train/test sets (single input → multi-output)
      var splitOutputs = new CatalogMap<SplitDataOutputs>()
        .Map(s => s.XTrain, catalog.XTrain)   // ✅ Both IEnumerable<FeatureRow>
        .Map(s => s.XTest, catalog.XTest)     // ✅ Both IEnumerable<FeatureRow>
        .Map(s => s.YTrain, catalog.YTrain)   // ✅ Both IEnumerable<decimal>
        .Map(s => s.YTest, catalog.YTest);    // ✅ Both IEnumerable<decimal>

      pipeline.AddNode<SplitDataNode, ModelInputSchema, SplitDataOutputs, ModelOptions>(
        input: catalog.ModelInputTable,       // ✅ Type-checked: ICatalogEntry<IEnumerable<ModelInputSchema>>
        output: splitOutputs,                 // ✅ Type-checked via CatalogMap
        name: "split_data_node",
        configure: node => node.Parameters = options ?? new ModelOptions()
      );

      // Node 2: Train OLS regression model (multi-input → single output)
      var trainInputs = new CatalogMap<TrainModelInputs>()
        .Map(x => x.XTrain, catalog.XTrain)   // ✅ Both IEnumerable<FeatureRow>
        .Map(x => x.YTrain, catalog.YTrain);  // ✅ Both IEnumerable<decimal>

      pipeline.AddNode<TrainModelNode, TrainModelInputs, LinearRegressionModel, NoParams>(
        input: trainInputs,                   // ✅ Type-checked via CatalogMap
        output: catalog.Regressor,            // ✅ Type-checked: ICatalogEntry<IEnumerable<LinearRegressionModel>>
        name: "train_model_node"
      );

      // Node 3: Evaluate OLS model (multi-input → single output)
      var evaluateInputs = new CatalogMap<EvaluateModelInputs>()
        .Map(x => x.Regressor, catalog.Regressor)  // ✅ Both IEnumerable<LinearRegressionModel>
        .Map(x => x.XTest, catalog.XTest)          // ✅ Both IEnumerable<FeatureRow>
        .Map(x => x.YTest, catalog.YTest);         // ✅ Both IEnumerable<decimal>

      pipeline.AddNode<EvaluateModelNode, EvaluateModelInputs, ModelMetrics, NoParams>(
        input: evaluateInputs,                // ✅ Type-checked via CatalogMap
        output: catalog.ModelMetrics,         // ✅ Type-checked: ICatalogEntry<IEnumerable<ModelMetrics>>
        name: "evaluate_model_node"
      // Optional: configure: node => node.Logger = logger
      );

      // Node 4: Cross-validation for R² distribution analysis
      pipeline.AddNode<CrossValidateModelNode, ModelInputSchema, CrossValidationResults, CrossValidationOptions>(
        input: catalog.ModelInputTable,       // ✅ Type-checked: ICatalogEntry<IEnumerable<ModelInputSchema>>
        output: catalog.CrossValidationResults, // ✅ Type-checked: ICatalogEntry<IEnumerable<CrossValidationResults>>
        name: "cross_validate_model_node",
        configure: node => node.Parameters = new CrossValidationOptions { NumFolds = 10 }
      );
    });
  }
}
