using Flowthru.Flow;
using SpaceflightsDistributed.DataProcessing.Data;
using SpaceflightsDistributed.DataProcessing.Data._03_Primary.Schemas;
using SpaceflightsDistributed.DataScience.Data;
using SpaceflightsDistributed.DataScience.Data._05_ModelInput.Schemas;
using SpaceflightsDistributed.DataScience.Data._06_Models.Schemas;
using SpaceflightsDistributed.DataScience.Data._07_ModelOutput.Schemas;
using SpaceflightsDistributed.DataScience.Flows.DataScience.Steps;

namespace SpaceflightsDistributed.DataScience.Flows.DataScience;

/// <summary>
/// Trains and evaluates a price prediction model using processed shuttle data.
/// </summary>
public static class DataScienceFlow
{
  public static BuiltFlow Create(
    DataProcessingCatalog dp,
    DataScienceCatalog ds,
    DataScienceFlowConfig config
  )
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
        input1: dp.ModelInputTable,
        output1: ds.TrainSplit,
        output2: ds.TestSplit
      );

      pipeline.AddStep<IEnumerable<TrainingData>, LinearRegressionModel>(
        label: "TrainModel",
        transform: TrainModelStep.Create(),
        input1: ds.TrainSplit,
        output1: ds.Regressor
      );

      pipeline.AddStep<
        LinearRegressionModel,
        IEnumerable<TestData>,
        ModelMetrics,
        IEnumerable<ModelPredictions>
      >(
        label: "EvaluateModel",
        transform: EvaluateModelStep.Create(),
        input1: ds.Regressor,
        input2: ds.TestSplit,
        output1: ds.ModelMetrics,
        output2: ds.ModelPredictions
      );
    });
  }
}
