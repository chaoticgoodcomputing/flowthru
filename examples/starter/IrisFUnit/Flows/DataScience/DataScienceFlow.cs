using Flowthru.Flow;
using IrisFUnit.Data;
using IrisFUnit.Data._05_ModelInput.Schemas;
using IrisFUnit.Data._06_Models.Schemas;
using IrisFUnit.Data._07_ModelOutput.Schemas;
using IrisFUnit.Data._08_Reporting.Schemas;
using IrisFUnit.Flows.DataScience.Steps;
using Microsoft.Extensions.Logging;

namespace IrisFUnit.Flows.DataScience;

/// <summary>
/// Creates the data science pipeline that trains and evaluates a
/// classification model. Per Phase 5/8 of the smart-caching RFC,
/// option records are exposed on the catalog as ordinary inputs and
/// wire into the per-step AddStep tuples like any other catalog
/// dependency — the AddStep arity therefore includes the
/// configuration-bound options alongside the data inputs and a
/// change to <c>appsettings.json</c> invalidates the affected
/// downstream cache automatically.
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
        transform: EvaluateModelStep.Create(logger),
        inputs: (catalog.Predictions, catalog.TestY),
        outputs: catalog.Metrics
      );
    });
  }
}
