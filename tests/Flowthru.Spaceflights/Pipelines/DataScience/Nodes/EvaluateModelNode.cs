using System.ComponentModel.DataAnnotations;
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
public class EvaluateModelNode : NodeBase<EvaluateModelInputs, ModelMetrics>
{
  // Note: Logger property is inherited from NodeBase and automatically available

  protected override Task<IEnumerable<ModelMetrics>> Transform(
      IEnumerable<EvaluateModelInputs> inputs)
  {
    // Extract the singleton input containing all catalog data
    var input = inputs.Single();
    var model = input.Regressor;
    var xTestData = input.XTest;
    var yTestData = input.YTest;

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

#region Node Artifacts (Colocated)

/// <summary>
/// Multi-input schema for EvaluateModelNode.
/// Bundles trained model with test features and targets for evaluation.
/// </summary>
public record EvaluateModelInputs
{
  /// <summary>
  /// Trained regression model
  /// </summary>
  [Required]
  public ITransformer Regressor { get; init; } = null!;

  /// <summary>
  /// Test features
  /// </summary>
  [Required]
  public IEnumerable<FeatureRow> XTest { get; init; } = null!;

  /// <summary>
  /// Test targets (prices)
  /// </summary>
  [Required]
  public IEnumerable<decimal> YTest { get; init; } = null!;
}

#endregion
