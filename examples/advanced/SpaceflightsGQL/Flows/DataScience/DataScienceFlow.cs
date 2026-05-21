using Flowthru.Flow;
using SpaceflightsGQL.Data;
using SpaceflightsGQL.Data._03_Primary.Schemas;
using SpaceflightsGQL.Data._05_ModelInput.Schemas;
using SpaceflightsGQL.Data._06_Models.Schemas;
using SpaceflightsGQL.Data._07_ModelOutput.Schemas;
using SpaceflightsGQL.Flows.DataScience.Steps;

namespace SpaceflightsGQL.Flows.DataScience;

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
