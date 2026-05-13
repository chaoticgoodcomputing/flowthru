using Flowthru.Data.Schema;

namespace SpaceflightsHybridCatalog.Data._06_Models.Schemas;

/// <summary>
/// Represents a trained linear regression model with coefficients and feature mappings.
/// </summary>
[FlowthruSchema]
public partial record LinearRegressionModel
{
  public double[] Coefficients { get; init; } = Array.Empty<double>();
  public double Intercept { get; init; }
  public string[] FeatureNames { get; init; } = Array.Empty<string>();
}
