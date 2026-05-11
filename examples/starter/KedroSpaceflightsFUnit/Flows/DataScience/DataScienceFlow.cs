using Flowthru.Flow;
using KedroSpaceflightsFUnit.Data;
using KedroSpaceflightsFUnit.Data._03_Primary.Schemas;
using KedroSpaceflightsFUnit.Data._05_ModelInput.Schemas;
using KedroSpaceflightsFUnit.Data._06_Models.Schemas;
using KedroSpaceflightsFUnit.Data._07_ModelOutput.Schemas;
using KedroSpaceflightsFUnit.Flows.DataScience.Steps;

namespace KedroSpaceflightsFUnit.Flows.DataScience;

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
