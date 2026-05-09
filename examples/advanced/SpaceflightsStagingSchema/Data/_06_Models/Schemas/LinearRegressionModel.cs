using Flowthru.Data.Schema;

namespace SpaceflightsStagingSchema.Data._06_Models.Schemas;

[FlowthruSchema]
public partial record LinearRegressionModel
{
  /// <summary>Auto-generated surrogate key.</summary>
  public int Id { get; init; }

  public double[] Coefficients { get; init; } = Array.Empty<double>();
  public double Intercept { get; init; }
  public string[] FeatureNames { get; init; } = Array.Empty<string>();
}
