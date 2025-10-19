using System.ComponentModel.DataAnnotations;
using Flowthru.Nodes;
using Flowthru.Spaceflights.Data.Schemas.Models;
using Microsoft.Extensions.Logging;
using MathNet.Numerics;

namespace Flowthru.Spaceflights.Pipelines.DataScience.Nodes;

/// <summary>
/// Evaluates the trained OLS regression model on test data and logs metrics.
/// Uses Math.NET Numerics GoodnessOfFit.RSquared() matching sklearn's r2_score.
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
    try
    {
      // Extract the singleton input containing all catalog data
      var input = inputs.Single();
      var model = input.Regressor.Single(); // Extract single model from collection
      var xTestData = input.XTest.ToList();
      var yTestData = input.YTest.ToList();

      // Make predictions using the OLS model
      var predictions = model.Predict(xTestData);
      var actualValues = yTestData.Select(y => (double)y).ToArray();

      // Calculate R² using Math.NET's GoodnessOfFit.RSquared
      // This uses the same formula as sklearn's r2_score: 1 - (SS_res / SS_tot)
      // Note: GoodnessOfFit.RSquared(modeledValues, observedValues)
      var r2Score = GoodnessOfFit.RSquared(predictions, actualValues);

      // Calculate Mean Absolute Error (MAE)
      var mae = predictions.Zip(actualValues, (pred, actual) => Math.Abs(pred - actual)).Average();

      // Calculate Root Mean Squared Error (RMSE)
      var mse = predictions.Zip(actualValues, (pred, actual) => Math.Pow(pred - actual, 2)).Average();
      var rmse = Math.Sqrt(mse);

      // Calculate Max Error
      var maxError = predictions.Zip(actualValues, (pred, actual) => Math.Abs(pred - actual)).Max();

      var metrics = new ModelMetrics
      {
        R2Score = r2Score,
        MeanAbsoluteError = mae,
        MaxError = maxError,
        RootMeanSquaredError = rmse
      };

      // Log results
      Logger?.LogInformation(
          "Model has a coefficient R² of {R2Score:F3} on test data.",
          metrics.R2Score);
      Logger?.LogInformation(
          "Mean Absolute Error: {MAE:F2}",
          metrics.MeanAbsoluteError);
      Logger?.LogInformation(
          "Max Error: {MaxError:F2}",
          metrics.MaxError);
      Logger?.LogInformation(
          "Root Mean Squared Error: {RMSE:F2}",
          metrics.RootMeanSquaredError);

      // Return as singleton collection
      return Task.FromResult(new[] { metrics }.AsEnumerable());
    }
    catch (Exception ex)
    {
      Logger?.LogError(ex, "Error in EvaluateModelNode: {Message}", ex.Message);
      throw;
    }
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
  /// Trained OLS regression model (singleton collection from catalog)
  /// </summary>
  [Required]
  public IEnumerable<LinearRegressionModel> Regressor { get; init; } = null!;

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
