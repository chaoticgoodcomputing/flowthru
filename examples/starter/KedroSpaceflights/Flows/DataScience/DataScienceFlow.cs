using Flowthru.Flow;
using KedroSpaceflights.Data;
using KedroSpaceflights.Data._03_Primary.Schemas;
using KedroSpaceflights.Data._05_ModelInput.Schemas;
using KedroSpaceflights.Data._06_Models.Schemas;
using KedroSpaceflights.Data._07_ModelOutput.Schemas;
using KedroSpaceflights.Flows.DataScience.Steps;

namespace KedroSpaceflights.Flows.DataScience;

/// <summary>
/// Creates the data science pipeline that trains and evaluates a price prediction model.
/// </summary>
public static class DataScienceFlow
{
  public static BuiltFlow Create(Catalog catalog, FlowConfig config)
  {
    return FlowBuilder.CreateFlow("DataScience", pipeline =>
    {
      pipeline.AddStep<
        IEnumerable<ModelInputTableSchema>,
        IEnumerable<TrainingData>,
        IEnumerable<TestData>
      >(
        label: "SplitData",
        transform: SplitDataStep.Create(config.ModelOptions),
        inputs: catalog.ModelInputTable,
        outputs: (catalog.TrainSplit, catalog.TestSplit)
      );

      pipeline.AddStep<IEnumerable<TrainingData>, LinearRegressionModel>(
        label: "TrainModel",
        transform: TrainModelStep.Create(),
        inputs: catalog.TrainSplit,
        outputs: catalog.Regressor
      );

      pipeline.AddStep<
        LinearRegressionModel,
        IEnumerable<TestData>,
        ModelMetrics,
        IEnumerable<ModelPredictions>
      >(
        label: "EvaluateModel",
        transform: EvaluateModelStep.Create(),
        inputs: (catalog.Regressor, catalog.TestSplit),
        outputs: (catalog.ModelMetrics, catalog.ModelPredictions)
      );
    });
  }
}
