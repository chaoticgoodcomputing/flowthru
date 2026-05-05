using Flowthru.Core.Flows;
using SpaceflightsStagingSchema.Data;

namespace SpaceflightsStagingSchema.Flows.DataScience;

/// <summary>
/// Builds the model input table as a deferred SQL join over the three
/// FK-constrained production tables, splits the joined view into train/test
/// sets, trains a regression model, and writes evaluation metrics + per-row
/// predictions back to production. Staging is never referenced.
/// </summary>
public static class DataScienceFlow
{
  public static Flow Create(ProductionCatalog production, FlowConfig config)
  {
    return FlowBuilder.CreateFlow(pipeline =>
    {
      pipeline.AddStep(
        label: "BuildModelInputTable",
        description: "Composes the model input table as a deferred SQL join over Companies, Shuttles, and Reviews.",
        transform: Steps.BuildModelInputTableStep.Create(),
        input: (production.Shuttles, production.Companies, production.Reviews),
        output: production.ModelInputTable
      );

      pipeline.AddStep(
        label: "SplitData",
        description: "Splits the model input view into training and test sets. Iteration triggers the SQL join.",
        transform: Steps.SplitDataStep.Create,
        input: (production.ModelInputTable, config.ModelOptions),
        output: (production.TrainSplit, production.TestSplit)
      );

      pipeline.AddStep(
        label: "TrainModel",
        description: "Trains a regression model to predict shuttle prices.",
        transform: Steps.TrainModelStep.Create(),
        input: production.TrainSplit,
        output: production.Regressor
      );

      pipeline.AddStep(
        label: "EvaluateModel",
        description: "Evaluates the trained model on the test set and computes metrics + predictions.",
        transform: Steps.EvaluateModelStep.Create(),
        input: (production.Regressor, production.TestSplit),
        output: (production.ModelMetrics, production.ModelPredictions)
      );
    });
  }
}
