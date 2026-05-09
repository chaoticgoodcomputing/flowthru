using Flowthru.Data.Schema;

namespace SpaceflightsPythonEFCore.Data._06_Models.Schemas;

/// <summary>
/// Trained linear regression model with coefficients and feature mappings.
/// Produced by the Python train_model node.
/// </summary>
[FlowthruSchema]
public partial record LinearRegressionModel
{
  public double[] Coefficients { get; init; } = Array.Empty<double>();
  public double Intercept { get; init; }
  public string[] FeatureNames { get; init; } = Array.Empty<string>();
}
