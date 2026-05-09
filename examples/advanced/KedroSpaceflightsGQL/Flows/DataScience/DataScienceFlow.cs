using Flowthru.Flow;
using KedroSpaceflightsGQL.Data;
using KedroSpaceflightsGQL.Data._03_Primary.Schemas;
using KedroSpaceflightsGQL.Data._05_ModelInput.Schemas;
using KedroSpaceflightsGQL.Data._06_Models.Schemas;
using KedroSpaceflightsGQL.Data._07_ModelOutput.Schemas;
using KedroSpaceflightsGQL.Flows.DataScience.Steps;

namespace KedroSpaceflightsGQL.Flows.DataScience;

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
        input1: catalog.ModelInputTable,
        output1: catalog.TrainSplit,
        output2: catalog.TestSplit
      );

      pipeline.AddStep<IEnumerable<TrainingData>, LinearRegressionModel>(
        label: "TrainModel",
        transform: TrainModelStep.Create(),
        input1: catalog.TrainSplit,
        output1: catalog.Regressor
      );

      pipeline.AddStep<
        LinearRegressionModel,
        IEnumerable<TestData>,
        ModelMetrics,
        IEnumerable<ModelPredictions>
      >(
        label: "EvaluateModel",
        transform: EvaluateModelStep.Create(),
        input1: catalog.Regressor,
        input2: catalog.TestSplit,
        output1: catalog.ModelMetrics,
        output2: catalog.ModelPredictions
      );
    });
  }
}
