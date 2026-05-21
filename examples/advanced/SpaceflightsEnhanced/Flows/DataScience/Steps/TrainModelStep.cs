using Flowthru.Data.Schema;
using Flowthru.Step;
using SpaceflightsEnhanced.Data._03_Primary.Schemas;
using SpaceflightsEnhanced.Data._04_Models.Schemas;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearRegression;

namespace SpaceflightsEnhanced.Flows.DataScience.Steps;

/// <summary>
/// Trains a linear regression model using ordinary least squares (OLS).
/// Uses Math.NET Numerics MultipleRegression.QR() which matches sklearn's LinearRegression.
/// Takes training features (x_train) and targets (y_train) as separate inputs.
/// </summary>
[FlowthruStep]
public static class TrainModelStep
{
  /// <summary>
  /// Creates a transformation function that trains a linear regression model.
  /// </summary>
  public static Func<
    (IEnumerable<FeatureRow> XTrain, IEnumerable<TargetValue> YTrain),
    Task<LinearRegressionModel>
  > Create()
  {
    return async (input) =>
    {
      var xTrainData = input.XTrain.ToList();
      var yTrainData = input.YTrain.ToList();

      // Build design matrix using centralized feature extraction from FeatureRow
      var dataPoints = xTrainData.Select(row => row.ToFeatureArray()).ToArray();

      // Convert target prices to double array
      var targets = yTrainData.Select(t => (double)t.Price).ToArray();

      // Train OLS regression using QR decomposition (same as sklearn's LinearRegression)
      // intercept: true adds a bias term automatically
      double[] coefficients = MultipleRegression.QR(dataPoints, targets, intercept: true);

      // Extract intercept and feature coefficients
      var intercept = coefficients[0];
      var featureCoefficients = coefficients.Skip(1).ToArray();

      var model = new LinearRegressionModel
      {
        Intercept = intercept,
        Coefficients = featureCoefficients,
        FeatureNames = FeatureRow.FeatureNames,
      };

      return await Task.FromResult(model);
    };
  }
}
