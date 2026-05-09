using Flowthru.Flow;
using SpaceflightsStagingSchema.Data;
using SpaceflightsStagingSchema.Data._02_Intermediate.Schemas;
using SpaceflightsStagingSchema.Data._03_Primary.Schemas;
using SpaceflightsStagingSchema.Data._05_ModelInput.Schemas;
using SpaceflightsStagingSchema.Data._06_Models.Schemas;
using SpaceflightsStagingSchema.Data._07_ModelOutput.Schemas;

namespace SpaceflightsStagingSchema.Flows.DataScience;

/// <summary>
/// Builds the model input table as a deferred SQL join over the three
/// FK-constrained production tables, splits the joined view into train/test
/// sets, trains a regression model, and writes evaluation metrics + per-row
/// predictions back to production.
/// </summary>
public static class DataScienceFlow
{
  public static BuiltFlow Create(ProductionCatalog production, FlowConfig config)
  {
    return FlowBuilder.CreateFlow("DataScience", pipeline =>
    {
      pipeline.AddStep<
        IEnumerable<PreprocessedShuttleSchema>,
        IEnumerable<PreprocessedCompanySchema>,
        IEnumerable<PreprocessedReviewSchema>,
        IEnumerable<ModelInputTableSchema>
      >(
        label: "BuildModelInputTable",
        transform: Steps.BuildModelInputTableStep.Create(),
        input1: production.Shuttles,
        input2: production.Companies,
        input3: production.Reviews,
        output1: production.ModelInputTable
      );

      pipeline.AddStep<
        IEnumerable<ModelInputTableSchema>,
        IEnumerable<TrainingData>,
        IEnumerable<TestData>
      >(
        label: "SplitData",
        transform: Steps.SplitDataStep.Create(config.ModelOptions),
        input1: production.ModelInputTable,
        output1: production.TrainSplit,
        output2: production.TestSplit
      );

      pipeline.AddStep<IEnumerable<TrainingData>, LinearRegressionModel>(
        label: "TrainModel",
        transform: Steps.TrainModelStep.Create(),
        input1: production.TrainSplit,
        output1: production.Regressor
      );

      pipeline.AddStep<
        LinearRegressionModel,
        IEnumerable<TestData>,
        ModelMetrics,
        IEnumerable<ModelPredictions>
      >(
        label: "EvaluateModel",
        transform: Steps.EvaluateModelStep.Create(),
        input1: production.Regressor,
        input2: production.TestSplit,
        output1: production.ModelMetrics,
        output2: production.ModelPredictions
      );
    });
  }
}
