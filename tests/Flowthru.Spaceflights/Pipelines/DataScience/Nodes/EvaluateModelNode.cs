using Flowthru.Nodes;
using Flowthru.Spaceflights.Data.Schemas.Models;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.Extensions.Logging;

namespace Flowthru.Spaceflights.Pipelines.DataScience.Nodes;

/// <summary>
/// Evaluates the trained model on test data and logs metrics.
/// This is a side-effect node that produces metrics but primarily logs results.
/// </summary>
public class EvaluateModelNode : Node<ITransformer, TrainTestSplit, ModelMetrics>
{
  private readonly ILogger<EvaluateModelNode>? _logger;

  public EvaluateModelNode(ILogger<EvaluateModelNode>? logger = null)
  {
    _logger = logger;
  }

  protected override Task<IEnumerable<ModelMetrics>> TransformInternal(
      IEnumerable<ITransformer> models,
      IEnumerable<TrainTestSplit> splits)
  {
    var model = models.Single();
    var split = splits.Single();

    var mlContext = new MLContext(seed: 0);

    // Convert test data to ML.NET format
    var testData = mlContext.Data.LoadFromEnumerable(split.XTest);

    // Make predictions
    var predictions = model.Transform(testData);

    // Evaluate metrics
    var regressionMetrics = mlContext.Regression.Evaluate(
        predictions,
        labelColumnName: "Label",
        scoreColumnName: "Score");

    var metrics = new ModelMetrics
    {
      R2Score = regressionMetrics.RSquared,
      MeanAbsoluteError = regressionMetrics.MeanAbsoluteError,
      MaxError = double.NaN, // ML.NET doesn't provide this directly
      RootMeanSquaredError = regressionMetrics.RootMeanSquaredError
    };

    // Log results
    _logger?.LogInformation(
        "Model has a coefficient R² of {R2Score:F3} on test data.",
        metrics.R2Score);
    _logger?.LogInformation(
        "Mean Absolute Error: {MAE:F2}",
        metrics.MeanAbsoluteError);
    _logger?.LogInformation(
        "Root Mean Squared Error: {RMSE:F2}",
        metrics.RootMeanSquaredError);

    // Return as singleton collection
    return Task.FromResult(new[] { metrics }.AsEnumerable());
  }
}
