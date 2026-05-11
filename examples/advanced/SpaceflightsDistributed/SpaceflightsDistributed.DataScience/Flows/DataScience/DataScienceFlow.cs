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
    var splitOptions = config.ModelOptions;
    var splitTransform = SplitDataStep.Create();

    return FlowBuilder.CreateFlow("DataScience", pipeline =>
    {
      pipeline.AddStep<
        IEnumerable<ModelInputTableSchema>,
        IEnumerable<TrainingData>,
        IEnumerable<TestData>
      >(
        label: "SplitData",
        transform: rawData => splitTransform((rawData, splitOptions)),
        inputs: dp.ModelInputTable,
        outputs: (ds.TrainSplit, ds.TestSplit)
      );

      pipeline.AddStep<IEnumerable<TrainingData>, LinearRegressionModel>(
        label: "TrainModel",
        transform: TrainModelStep.Create(),
        inputs: ds.TrainSplit,
        outputs: ds.Regressor
      );

      pipeline.AddStep<
        LinearRegressionModel,
        IEnumerable<TestData>,
        ModelMetrics,
        IEnumerable<ModelPredictions>
      >(
        label: "EvaluateModel",
        transform: EvaluateModelStep.Create(),
        inputs: (ds.Regressor, ds.TestSplit),
        outputs: (ds.ModelMetrics, ds.ModelPredictions)
      );
    });
  }
}
