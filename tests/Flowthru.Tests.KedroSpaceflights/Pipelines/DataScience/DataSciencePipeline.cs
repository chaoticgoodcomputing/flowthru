using Flowthru.Nodes;
using Flowthru.Pipelines;
using Flowthru.Pipelines.Mapping;
using Flowthru.Tests.KedroSpaceflights.Data;
using Flowthru.Tests.KedroSpaceflights.Data.Schemas.Models;
using Flowthru.Tests.KedroSpaceflights.Data.Schemas.Processed;
using Flowthru.Tests.KedroSpaceflights.Pipelines.DataScience.Nodes;
using Microsoft.ML;

namespace Flowthru.Tests.KedroSpaceflights.Pipelines.DataScience;

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
public static class DataSciencePipeline {
  public static Pipeline Create(SpaceflightsCatalog catalog, ModelOptions options) {
    return PipelineBuilder.CreatePipeline(pipeline => {
      // Node 1: Split data into train/test sets (single input → multi-output)
      var splitOutputs = pipeline.AddNode<SplitDataNode, ModelInputSchema, SplitDataOutputs, ModelOptions>(
        name: "SplitData",
        input: catalog.ModelInputTable,
        output: new CatalogMap<SplitDataOutputs>()
          .Map(s => s.XTrain, catalog.XTrain)
          .Map(s => s.XTest, catalog.XTest)
          .Map(s => s.YTrain, catalog.YTrain)
          .Map(s => s.YTest, catalog.YTest),
        configure: node => node.Parameters = options
      );

      // Node 2: Train OLS regression model (multi-input → single output)
      pipeline.AddNode<TrainModelNode, TrainModelInputs, LinearRegressionModel, NoParams>(
        input: new CatalogMap<TrainModelInputs>()
          .Map(x => x.XTrain, catalog.XTrain)
          .Map(x => x.YTrain, catalog.YTrain),
        output: catalog.Regressor,
        name: "TrainModel"
      );

      // Node 3: Evaluate OLS model (multi-input → single output)
      var evaluateInputs = pipeline.AddNode<EvaluateModelNode, EvaluateModelInputs, ModelMetrics, NoParams>(
        name: "EvaluateModel",
        input: new CatalogMap<EvaluateModelInputs>()
          .Map(x => x.Regressor, catalog.Regressor)
          .Map(x => x.XTest, catalog.XTest)
          .Map(x => x.YTest, catalog.YTest),
        output: catalog.ModelMetrics
      );

      // Node 4: Cross-validation for R² distribution analysis and comparison to the original Kedro
      // spaceflights example.
      pipeline.AddNode<CrossValidateModelNode, ModelInputSchema, CrossValidationResults, CrossValidationOptions>(
        name: "CrossValidateAndCompareToKedroSource",
        input: catalog.ModelInputTable,
        output: catalog.CrossValidationResults,
        configure: node => node.Parameters = new CrossValidationOptions {
          NumFolds = 10, // Standard 10-fold cross-validation  
          BaseSeed = 42, // A magic number, nothing up our sleeves!
          KedroReferenceR2Score = 0.387f  // Baseline comparison to the seeded run of the
                                          // unmodified Kedro implementation in Python.
        }
      );
    });
  }
}
