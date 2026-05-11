using Flowthru.Flow;
using KedroSpaceflightsCustom.Data;
using KedroSpaceflightsCustom.Data._03_Primary.Schemas;
using KedroSpaceflightsCustom.Data._04_Models.Schemas;
using KedroSpaceflightsCustom.Data._05_ModelOutput.Schemas;
using KedroSpaceflightsCustom.Data._06_Reporting.Schemas;
using KedroSpaceflightsCustom.Flows.DataEvaluation.Steps;

namespace KedroSpaceflightsCustom.Flows.DataEvaluation;

/// <summary>
/// Data evaluation pipeline that evaluates the trained OLS model and runs cross-validation.
/// </summary>
public static class DataEvaluationFlow
{
  public static BuiltFlow Create(Catalog catalog, FlowConfig config)
  {
    var crossValidationParams = config.CrossValidationParams;
    var crossValidateTransform = CrossValidateModelStep.Create();

    return FlowBuilder.CreateFlow("DataEvaluation", pipeline =>
    {
      pipeline.AddStep<
        LinearRegressionModel,
        IEnumerable<FeatureRow>,
        IEnumerable<TargetValue>,
        ModelMetrics,
        IEnumerable<ModelPredictions>
      >(
        label: "EvaluateOLSModel",
        transform: EvaluateModelStep.Create(),
        inputs: (catalog.Regressor, catalog.XTest, catalog.YTest),
        outputs: (catalog.ModelMetrics, catalog.ModelPredictions)
      );

      pipeline.AddStep<IEnumerable<ModelInputSchema>, CrossValidationResults>(
        label: "PerformCrossValidatedOLSRegressionTest",
        transform: data => crossValidateTransform((data, crossValidationParams)),
        inputs: catalog.ModelInputTable,
        outputs: catalog.CrossValidationResults
      );
    });
  }
}
