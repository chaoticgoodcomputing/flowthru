using Flowthru.Flow;
using SpaceflightsEnhanced.Data;
using SpaceflightsEnhanced.Data._03_Primary.Schemas;
using SpaceflightsEnhanced.Data._04_Models.Schemas;
using SpaceflightsEnhanced.Flows.DataScience.Steps;

namespace SpaceflightsEnhanced.Flows.DataScience;

/// <summary>
/// Data science pipeline that splits data and trains the regression model.
/// </summary>
public static class DataScienceFlow
{
  public static BuiltFlow Create(Catalog catalog)
  {
    return FlowBuilder.CreateFlow("DataScience", pipeline =>
    {
      pipeline.AddStep<
        IEnumerable<ModelInputSchema>,
        CreateTestTrainSplitStep.TestTrainSplitParams,
        IEnumerable<FeatureRow>,
        IEnumerable<FeatureRow>,
        IEnumerable<TargetValue>,
        IEnumerable<TargetValue>
      >(
        label: "CreateTestTrainSplitDatasets",
        transform: CreateTestTrainSplitStep.Create(),
        inputs: (catalog.ModelInputTable, catalog.ModelParams),
        outputs: (catalog.XTrain, catalog.XTest, catalog.YTrain, catalog.YTest)
      );

      pipeline.AddStep<IEnumerable<FeatureRow>, IEnumerable<TargetValue>, LinearRegressionModel>(
        label: "TrainOLSModel",
        transform: TrainModelStep.Create(),
        inputs: (catalog.XTrain, catalog.YTrain),
        outputs: catalog.Regressor
      );
    });
  }
}
