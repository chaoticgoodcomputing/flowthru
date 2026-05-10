using Flowthru.Flow;
using KedroSpaceflightsCustom.Data;
using KedroSpaceflightsCustom.Data._03_Primary.Schemas;
using KedroSpaceflightsCustom.Data._04_Models.Schemas;
using KedroSpaceflightsCustom.Flows.DataScience.Steps;

namespace KedroSpaceflightsCustom.Flows.DataScience;

/// <summary>
/// Data science pipeline that splits data and trains the regression model.
/// </summary>
public static class DataScienceFlow
{
  public static BuiltFlow Create(Catalog catalog, FlowConfig config)
  {
    var modelParams = config.ModelParams;
    var splitTransform = CreateTestTrainSplitStep.Create();

    return FlowBuilder.CreateFlow("DataScience", pipeline =>
    {
      pipeline.AddStep<
        IEnumerable<ModelInputSchema>,
        IEnumerable<FeatureRow>,
        IEnumerable<FeatureRow>,
        IEnumerable<TargetValue>,
        IEnumerable<TargetValue>
      >(
        label: "CreateTestTrainSplitDatasets",
        transform: data => splitTransform((data, modelParams)),
        inputs: catalog.ModelInputTable,
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
