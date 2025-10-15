using Flowthru.Nodes;
using Flowthru.Spaceflights.Data.Schemas.Models;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.Extensions.Logging;

namespace Flowthru.Spaceflights.Pipelines.DataScience.Nodes;

/// <summary>
/// Evaluates the trained model on test data and logs metrics.
/// This is a side-effect node that produces metrics but primarily logs results.
/// 
/// Multi-input node - receives model (regressor), test features (x_test), 
/// and test targets (y_test) as separate catalog entries.
/// 
/// Uses property injection for ILogger to maintain parameterless constructor
/// for type reference instantiation (required for distributed/parallel execution).
/// </summary>
public class EvaluateModelNode : Node<ITransformer, IEnumerable<FeatureRow>, IEnumerable<decimal>, ModelMetrics>
{
  /// <summary>
  /// Optional logger for outputting model evaluation metrics.
  /// Can be set via property injection from DI container.
  /// </summary>
  public ILogger<EvaluateModelNode>? Logger { get; set; }

  protected override Task<IEnumerable<ModelMetrics>> Transform(
      IEnumerable<ITransformer> models,
      IEnumerable<IEnumerable<FeatureRow>> xTest,
      IEnumerable<IEnumerable<decimal>> yTest)
  {
    var model = models.Single();
    var xTestData = xTest.Single();
    var yTestData = yTest.Single();

    var mlContext = new MLContext(seed: 0);

    // Convert test data to ML.NET format
    var testData = mlContext.Data.LoadFromEnumerable(xTestData);

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
    Logger?.LogInformation(
        "Model has a coefficient R² of {R2Score:F3} on test data.",
        metrics.R2Score);
    Logger?.LogInformation(
        "Mean Absolute Error: {MAE:F2}",
        metrics.MeanAbsoluteError);
    Logger?.LogInformation(
        "Root Mean Squared Error: {RMSE:F2}",
        metrics.RootMeanSquaredError);

    // Return as singleton collection
    return Task.FromResult(new[] { metrics }.AsEnumerable());
  }
}
