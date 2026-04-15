using Flowthru.Core.Steps;
using KedroSpaceflights.Custom.Data._03_Primary.Schemas;
using KedroSpaceflights.Custom.Data._04_Models.Schemas;
using KedroSpaceflights.Custom.Data._05_ModelOutput.Schemas;
using KedroSpaceflights.Custom.Flows.DataScience.Steps;
using MathNet.Numerics;
using Microsoft.Extensions.Logging;

namespace KedroSpaceflights.Custom.Flows.DataEvaluation.Steps;

/// <summary>
/// Evaluates the trained OLS regression model on test data and logs metrics.
/// Uses Math.NET Numerics GoodnessOfFit.RSquared() matching sklearn's r2_score.
/// </summary>
[FlowthruStep]
public static class EvaluateModelStep
{
    /// <summary>
    /// Creates a transformation function that evaluates a trained model.
    /// </summary>
    /// <param name="logger">Logger for outputting model metrics</param>
    public static Func<
      (
        LinearRegressionModel Regressor,
        IEnumerable<FeatureRow> XTest,
        IEnumerable<TargetValue> YTest
      ),
      Task<(ModelMetrics Metrics, IEnumerable<ModelPredictions> Predictions)>
    > Create(ILogger? logger = null)
    {
        return async (input) =>
        {
            var model = input.Regressor; // Model is singleton, no unwrapping needed
            var xTestData = input.XTest.ToList();
            var yTestData = input.YTest.ToList();

            // Make predictions using the OLS model
            var predictions = model.Predict(xTestData);
            var actualValues = yTestData.Select(t => (double)t.Price).ToArray();

            // Calculate R² using Math.NET's GoodnessOfFit.RSquared
            // This uses the same formula as sklearn's r2_score: 1 - (SS_res / SS_tot)
            // Note: GoodnessOfFit.RSquared(modeledValues, observedValues)
            var r2Score = GoodnessOfFit.RSquared(predictions, actualValues);

            // Calculate Mean Absolute Error (MAE)
            var mae = predictions.Zip(actualValues, (pred, actual) => Math.Abs(pred - actual)).Average();

            // Calculate Root Mean Squared Error (RMSE)
            var mse = predictions
          .Zip(actualValues, (pred, actual) => Math.Pow(pred - actual, 2))
          .Average();
            var rmse = Math.Sqrt(mse);

            // Calculate Max Error
            var maxError = predictions.Zip(actualValues, (pred, actual) => Math.Abs(pred - actual)).Max();

            var metrics = new ModelMetrics
            {
                R2Score = r2Score,
                MeanAbsoluteError = mae,
                MaxError = maxError,
                RootMeanSquaredError = rmse,
            };

            // Log results
            logger?.LogInformation(
          "Model has a coefficient R² of {R2Score:F3} on test data.",
          metrics.R2Score
        );
            logger?.LogInformation("Mean Absolute Error: {MAE:F2}", metrics.MeanAbsoluteError);
            logger?.LogInformation("Max Error: {MaxError:F2}", metrics.MaxError);
            logger?.LogInformation("Root Mean Squared Error: {RMSE:F2}", metrics.RootMeanSquaredError);

            var predictionsCollection = predictions.Select(
          (pred, index) =>
            new ModelPredictions { Actual = (double)yTestData[index].Price, Predicted = pred }
        );

            return await Task.FromResult((Metrics: metrics, Predictions: predictionsCollection));
        };
    }
}
