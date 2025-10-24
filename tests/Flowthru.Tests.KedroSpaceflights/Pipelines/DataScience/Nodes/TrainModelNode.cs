using System.ComponentModel.DataAnnotations;
using Flowthru.Nodes;
using Flowthru.Tests.KedroSpaceflights.Data.Schemas.Models;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearRegression;

namespace Flowthru.Tests.KedroSpaceflights.Pipelines.DataScience.Nodes;

/// <summary>
/// Trains a linear regression model using ordinary least squares (OLS).
/// Uses Math.NET Numerics MultipleRegression.QR() which matches sklearn's LinearRegression.
/// Takes training features (x_train) and targets (y_train) as separate inputs.
/// 
/// Multi-input node - receives two catalog entries independently.
/// Stateless node with implicit parameterless constructor,
/// compatible with type reference instantiation for distributed/parallel execution.
/// </summary>
public class TrainModelNode : NodeBase<TrainModelInputs, LinearRegressionModel> {
  protected override Task<IEnumerable<LinearRegressionModel>> Transform(
      IEnumerable<TrainModelInputs> inputs) {
    // Extract the singleton input containing all catalog data
    var input = inputs.Single();
    var xTrainData = input.XTrain.ToList();
    var yTrainData = input.YTrain.ToList();

    // Build design matrix using centralized feature extraction from FeatureRow
    var dataPoints = xTrainData.Select(row => row.ToFeatureArray()).ToArray();

    // Convert target prices to double array
    var targets = yTrainData.Select(p => (double)p).ToArray();

    // Train OLS regression using QR decomposition (same as sklearn's LinearRegression)
    // intercept: true adds a bias term automatically
    double[] coefficients = MultipleRegression.QR(dataPoints, targets, intercept: true);

    // Extract intercept and feature coefficients
    var intercept = coefficients[0];
    var featureCoefficients = coefficients.Skip(1).ToArray();

    var model = new LinearRegressionModel {
      Intercept = intercept,
      Coefficients = featureCoefficients,
      FeatureNames = FeatureRow.FeatureNames
    };

    // Return as singleton collection
    return Task.FromResult(new[] { model }.AsEnumerable());
  }
}

#region Node Artifacts (Colocated)

/// <summary>
/// Multi-input schema for TrainModelNode.
/// Bundles training features and targets for model training.
/// </summary>
public record TrainModelInputs {
  /// <summary>
  /// Training features
  /// </summary>
  [Required]
  public IEnumerable<FeatureRow> XTrain { get; init; } = null!;

  /// <summary>
  /// Training targets (prices)
  /// </summary>
  [Required]
  public IEnumerable<decimal> YTrain { get; init; } = null!;
}

/// <summary>
/// Trained ordinary least squares linear regression model.
/// Contains intercept and feature coefficients.
/// </summary>
public record LinearRegressionModel {
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
  public double Predict(double[] features) {
    if (features.Length != Coefficients.Length) {
      throw new ArgumentException($"Expected {Coefficients.Length} features, got {features.Length}");
    }

    var prediction = Intercept;
    for (int i = 0; i < features.Length; i++) {
      prediction += Coefficients[i] * features[i];
    }
    return prediction;
  }

  /// <summary>
  /// Predict values for multiple feature rows using centralized feature extraction.
  /// </summary>
  public double[] Predict(IEnumerable<FeatureRow> rows) {
    return rows.Select(row => Predict(row.ToFeatureArray())).ToArray();
  }
}

#endregion
