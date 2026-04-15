using Flowthru.Core.Abstractions;

namespace KedroSpaceflightsSpark.Data._06_Models.Schemas;

[FlowthruSchema]
public partial record LinearRegressionModel
{
    public double[] Coefficients { get; init; } = Array.Empty<double>();
    public double Intercept { get; init; }
    public string[] FeatureNames { get; init; } = Array.Empty<string>();
}
