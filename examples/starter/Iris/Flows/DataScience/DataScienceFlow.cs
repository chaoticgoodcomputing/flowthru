using Flowthru.Flow;
using Iris.Data;
using Iris.Data._05_ModelInput.Schemas;
using Iris.Data._06_Models.Schemas;
using Iris.Data._07_ModelOutput.Schemas;
using Iris.Data._08_Reporting.Schemas;
using Iris.Flows.DataScience.Steps;
using Microsoft.Extensions.Logging;

namespace Iris.Flows.DataScience;

/// <summary>
/// Creates the data science pipeline that trains and evaluates a classification model.
/// </summary>
public static class DataScienceFlow
{
  public static BuiltFlow Create(Catalog catalog, ILogger logger)
  {
    return FlowBuilder.CreateFlow("DataScience", pipeline =>
    {
      pipeline.AddStep<
        IEnumerable<FeatureVectorSchema>,
        IEnumerable<TargetLabelSchema>,
        TrainModelStep.Options,
        ModelWeightsSchema
      >(
        label: "TrainModel",
        transform: TrainModelStep.Create(logger),
        inputs: (catalog.TrainX, catalog.TrainY, catalog.TrainModelOptions),
        outputs: catalog.IrisModel
      );

      pipeline.AddStep<ModelWeightsSchema, IEnumerable<FeatureVectorSchema>, IEnumerable<PredictionSchema>>(
        label: "Predict",
        transform: PredictStep.Create(),
        inputs: (catalog.IrisModel, catalog.TestX),
        outputs: catalog.Predictions
      );

      pipeline.AddStep<IEnumerable<PredictionSchema>, IEnumerable<TargetLabelSchema>, MetricsSchema>(
        label: "Evaluate",
        transform: EvaluateModelStep.Create(logger),
        inputs: (catalog.Predictions, catalog.TestY),
        outputs: catalog.Metrics
      );
    });
  }
}
