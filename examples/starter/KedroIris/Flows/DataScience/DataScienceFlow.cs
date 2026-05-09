using Flowthru.Flow;
using KedroIris.Data;
using KedroIris.Data._05_ModelInput.Schemas;
using KedroIris.Data._06_Models.Schemas;
using KedroIris.Data._07_ModelOutput.Schemas;
using KedroIris.Data._08_Reporting.Schemas;
using KedroIris.Flows.DataScience.Steps;

namespace KedroIris.Flows.DataScience;

/// <summary>
/// Creates the data science pipeline that trains and evaluates a classification model.
/// </summary>
public static class DataScienceFlow
{
  public static BuiltFlow Create(Catalog catalog, FlowConfig config)
  {
    var trainOptions = config.TrainOptions;
    var trainTransform = TrainModelStep.Create();

    return FlowBuilder.CreateFlow("DataScience", pipeline =>
    {
      pipeline.AddStep<
        IEnumerable<FeatureVectorSchema>,
        IEnumerable<TargetLabelSchema>,
        ModelWeightsSchema
      >(
        label: "TrainModel",
        transform: pair =>
        {
          var (trainX, trainY) = pair;
          return trainTransform((trainX, trainY, trainOptions));
        },
        input1: catalog.TrainX,
        input2: catalog.TrainY,
        output1: catalog.IrisModel
      );

      pipeline.AddStep<ModelWeightsSchema, IEnumerable<FeatureVectorSchema>, IEnumerable<PredictionSchema>>(
        label: "Predict",
        transform: PredictStep.Create(),
        input1: catalog.IrisModel,
        input2: catalog.TestX,
        output1: catalog.Predictions
      );

      pipeline.AddStep<IEnumerable<PredictionSchema>, IEnumerable<TargetLabelSchema>, MetricsSchema>(
        label: "Evaluate",
        transform: EvaluateModelStep.Create(),
        input1: catalog.Predictions,
        input2: catalog.TestY,
        output1: catalog.Metrics
      );
    });
  }
}
