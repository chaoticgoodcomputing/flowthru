using Flowthru.Flow;
using KedroIrisFUnit.Data;
using KedroIrisFUnit.Data._05_ModelInput.Schemas;
using KedroIrisFUnit.Data._06_Models.Schemas;
using KedroIrisFUnit.Data._07_ModelOutput.Schemas;
using KedroIrisFUnit.Data._08_Reporting.Schemas;
using KedroIrisFUnit.Flows.DataScience.Steps;

namespace KedroIrisFUnit.Flows.DataScience;

/// <summary>
/// Creates the data science pipeline that trains and evaluates a
/// classification model. Per §2.6, the flow factory closes over the
/// <see cref="FlowConfig"/> value (a DI-resolved "params catalog"); the
/// per-step <c>transform</c> lambda burns the options in via closure
/// capture, so the flow's data dependencies stay typed end-to-end and
/// the AddStep arity matches the data shape (no synthetic
/// in-memory option items polluting the DAG).
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
        inputs: (catalog.TrainX, catalog.TrainY),
        outputs: catalog.IrisModel
      );

      pipeline.AddStep<
        ModelWeightsSchema,
        IEnumerable<FeatureVectorSchema>,
        IEnumerable<PredictionSchema>
      >(
        label: "Predict",
        transform: PredictStep.Create(),
        inputs: (catalog.IrisModel, catalog.TestX),
        outputs: catalog.Predictions
      );

      pipeline.AddStep<
        IEnumerable<PredictionSchema>,
        IEnumerable<TargetLabelSchema>,
        MetricsSchema
      >(
        label: "Evaluate",
        transform: EvaluateModelStep.Create(),
        inputs: (catalog.Predictions, catalog.TestY),
        outputs: catalog.Metrics
      );
    });
  }
}
