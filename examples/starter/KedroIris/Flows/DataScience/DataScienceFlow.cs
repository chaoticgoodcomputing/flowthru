using Flowthru.Core.Flows;
using KedroIris.Data;
using KedroIris.Flows.DataScience.Steps;

namespace KedroIris.Flows.DataScience;

/// <summary>
/// Creates the data science pipeline that trains and evaluates a classification model.
/// </summary>
public static class DataScienceFlow
{
  /// <summary>
  /// Creates the data science pipeline.
  /// </summary>
  /// <param name="catalog">The data catalog containing input and output entries.</param>
  /// <param name="config">Configuration catalog providing pipeline parameters.</param>
  /// <returns>A configured pipeline that produces a trained model, predictions, and metrics.</returns>
  public static Flow Create(Catalog catalog, FlowConfig config)
  {
    return FlowBuilder.CreateFlow(pipeline =>
    {
      pipeline.AddStep(
        label: "TrainModel",
        description: "Trains a multi-class logistic regression model using gradient descent.",
        transform: TrainModelStep.Create,
        input: (catalog.TrainX, catalog.TrainY, config.TrainOptions),
        output: catalog.IrisModel
      );

      pipeline.AddStep(
        label: "Predict",
        description: "Predicts species classifications for the test set using the trained model.",
        transform: PredictStep.Create(),
        input: (catalog.IrisModel, catalog.TestX),
        output: catalog.Predictions
      );

      pipeline.AddStep(
        label: "Evaluate",
        description: "Evaluates prediction accuracy and saves metrics to the reporting layer.",
        transform: EvaluateModelStep.Create(),
        input: (catalog.Predictions, catalog.TestY),
        output: catalog.Metrics
      );
    });
  }
}
