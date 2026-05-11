using Flowthru.Flow;
using SpaceflightsEFCore.Data;
using SpaceflightsEFCore.Data._03_Primary.Schemas;
using SpaceflightsEFCore.Data._05_ModelInput.Schemas;
using SpaceflightsEFCore.Data._06_Models.Schemas;
using SpaceflightsEFCore.Data._07_ModelOutput.Schemas;
using SpaceflightsEFCore.Flows.DataScience.Steps;

namespace SpaceflightsEFCore.Flows.DataScience;

/// <summary>
/// Data science pipeline: trains and evaluates a price-prediction model.
/// Closes over <see cref="FlowConfig.ModelOptions"/> at flow-construction
/// time per §2.6 — config values are catalog-resolved properties, not
/// catalog items.
/// </summary>
public static class DataScienceFlow
{
  public static BuiltFlow Create(Catalog catalog, FlowConfig config)
  {
    var modelOptions = config.ModelOptions;
    var splitTransform = SplitDataStep.Create();
    var trainTransform = TrainModelStep.Create();
    var evaluateTransform = EvaluateModelStep.Create();

    return FlowBuilder.CreateFlow("DataScience", pipeline =>
    {
      pipeline.AddStep<
        IEnumerable<ModelInputTableSchema>,
        IEnumerable<TrainingData>,
        IEnumerable<TestData>
      >(
        label: "SplitData",
        transform: data => splitTransform((data, modelOptions)),
        inputs: catalog.ModelInputTable,
        outputs: (catalog.TrainSplit, catalog.TestSplit)
      );

      pipeline.AddStep<IEnumerable<TrainingData>, LinearRegressionModel>(
        label: "TrainModel",
        transform: trainTransform,
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
        transform: evaluateTransform,
        inputs: (catalog.Regressor, catalog.TestSplit),
        outputs: (catalog.ModelMetrics, catalog.ModelPredictions)
      );
    });
  }
}
