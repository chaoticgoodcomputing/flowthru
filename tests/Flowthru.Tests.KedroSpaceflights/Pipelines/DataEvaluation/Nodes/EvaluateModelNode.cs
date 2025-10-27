using System.ComponentModel.DataAnnotations;
using System.Linq;
using Flowthru.Nodes;
using Flowthru.Tests.KedroSpaceflights.Data.Schemas.Models;
using Flowthru.Tests.KedroSpaceflights.Pipelines.DataScience.Nodes;
using MathNet.Numerics;
using Microsoft.Extensions.Logging;

namespace Flowthru.Tests.KedroSpaceflights.Pipelines.DataEvaluation.Nodes;

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
public class EvaluateModelNode
  : NodeBase<
      (LinearRegressionModel Regressor,
       IEnumerable<FeatureRow> XTest,
       IEnumerable<decimal> YTest),
      (IEnumerable<ModelMetrics> Metrics, IEnumerable<ModelPredictions> Predictions),
      NoParams> {
  // Note: Logger property is inherited from NodeBase and automatically available

  protected override Task<(
    IEnumerable<ModelMetrics> Metrics,
    IEnumerable<ModelPredictions> Predictions
  )> Transform(
      (LinearRegressionModel Regressor,
       IEnumerable<FeatureRow> XTest,
       IEnumerable<decimal> YTest) input) {
    var model = input.Regressor; // Model is singleton, no unwrapping needed
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

    var metrics = new ModelMetrics {
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

    // Return tuple output (not wrapped in IEnumerable)
    var metricsCollection = (IEnumerable<ModelMetrics>)new[] { metrics };
    var predictionsCollection = predictions.Select((pred, index) => new ModelPredictions {
      Actual = (double)yTestData[index],
      Predicted = pred,
    });

    return Task.FromResult((Metrics: metricsCollection, Predictions: predictionsCollection));
  }
}
