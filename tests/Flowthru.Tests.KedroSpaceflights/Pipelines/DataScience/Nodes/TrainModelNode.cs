using Flowthru.Tests.KedroSpaceflights.Data.Schemas.Models;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearRegression;

namespace Flowthru.Tests.KedroSpaceflights.Pipelines.DataScience.Nodes;

/// <summary>
/// Trains a linear regression model using ordinary least squares (OLS).
/// Uses Math.NET Numerics MultipleRegression.QR() which matches sklearn's LinearRegression.
/// Takes training features (x_train) and targets (y_train) as separate inputs.
/// </summary>
public static class TrainModelNode
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

#region Node Artifacts (Colocated)
/// Trained ordinary least squares linear regression model.
/// Contains intercept and feature coefficients.
/// </summary>
public record LinearRegressionModel
{
  /// <summary>
  /// Model intercept (bias term)
  /// </summary>
  public double Intercept { get; init; }

  /// <summary>
  /// Feature coefficients in order of features
  /// </summary>
  public double[] Coefficients { get; init; } = Array.Empty<double>();

  /// <summary>
  /// Feature names corresponding to coefficients
  /// </summary>
  public string[] FeatureNames { get; init; } = Array.Empty<string>();

  /// <summary>
  /// Predict a single value given feature values
  /// </summary>
  public double Predict(double[] features)
  {
    if (features.Length != Coefficients.Length)
    {
      throw new ArgumentException(
        $"Expected {Coefficients.Length} features, got {features.Length}"
      );
    }

    var prediction = Intercept;
    for (int i = 0; i < features.Length; i++)
    {
      prediction += Coefficients[i] * features[i];
    }
    return prediction;
  }

  /// <summary>
  /// Predict values for multiple feature rows using centralized feature extraction.
  /// </summary>
  public double[] Predict(IEnumerable<FeatureRow> rows)
  {
    return rows.Select(row => Predict(row.ToFeatureArray())).ToArray();
  }
}

#endregion
