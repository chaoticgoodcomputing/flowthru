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
/// Options come from the catalog as configuration-bound items, so a
/// change to <c>Flowthru:Flows:DataScience:ModelOptions</c> in
/// <c>appsettings.json</c> invalidates the affected downstream cache
/// automatically (Phase 5/8 of the smart-caching RFC).
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
