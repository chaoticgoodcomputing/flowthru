using Flowthru.Abstractions;
using KedroSpaceflights.Custom.Data._03_Primary.Schemas;

namespace KedroSpaceflights.Custom.Data._04_Models.Schemas;

/// <summary>
/// Trained ordinary least squares linear regression model.
/// Contains intercept and feature coefficients.
/// </summary>
public record LinearRegressionModel : INestedSchema, IStructuredSerializable
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
