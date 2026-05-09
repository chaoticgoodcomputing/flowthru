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
  public static BuiltFlow Create(Catalog catalog, FlowConfig config)
  {
    var modelOptions = config.ModelOptions;
    var splitTransform = SplitDataStep.Create();

    return FlowBuilder.CreateFlow("DataScience", pipeline =>
    {
      pipeline.AddStep<
        IEnumerable<ModelInputTableSchema>,
        IEnumerable<TrainingData>,
        IEnumerable<TestData>
      >(
        label: "SplitData",
        transform: rawData => splitTransform((rawData, modelOptions)),
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
