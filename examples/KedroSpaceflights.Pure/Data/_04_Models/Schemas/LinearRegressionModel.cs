using Flowthru.Abstractions;

namespace KedroSpaceflights.Pure.Data._04_Models.Schemas;

public record LinearRegressionModel : IStructuredSerializable
{
  public double[] Coefficients { get; init; } = Array.Empty<double>();
  public double Intercept { get; init; }
  public string[] FeatureNames { get; init; } = Array.Empty<string>();
}
