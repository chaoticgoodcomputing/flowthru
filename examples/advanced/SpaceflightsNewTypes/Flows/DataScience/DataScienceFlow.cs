using Flowthru.Flow;
using SpaceflightsNewTypes.Data;
using SpaceflightsNewTypes.Data._03_Primary.Schemas;
using SpaceflightsNewTypes.Data._05_ModelInput.Schemas;
using SpaceflightsNewTypes.Data._06_Models.Schemas;
using SpaceflightsNewTypes.Data._07_ModelOutput.Schemas;
using SpaceflightsNewTypes.Flows.DataScience.Steps;

namespace SpaceflightsNewTypes.Flows.DataScience;

/// <summary>
/// Creates the data science pipeline that trains and evaluates a price prediction model.
/// </summary>
public static class DataScienceFlow
{
  public static BuiltFlow Create(Catalog catalog)
  {
    return FlowBuilder.CreateFlow("DataScience", pipeline =>
    {
      pipeline.AddStep<
        IEnumerable<ModelInputTableSchema>,
        SplitDataStep.ModelOptions,
        IEnumerable<TrainingData>,
        IEnumerable<TestData>
      >(
        label: "SplitData",
        transform: SplitDataStep.Create(),
        inputs: (catalog.ModelInputTable, catalog.ModelOptions),
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
