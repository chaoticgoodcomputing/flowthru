using Flowthru.Nodes;
using Flowthru.Tests.KedroSpaceflights.Data.Schemas.Processed;
using Microsoft.Extensions.Logging;

namespace Flowthru.Tests.KedroSpaceflights.Pipelines.DataValidation.Nodes;

/// <summary>
/// Diagnostic node that generates synthetic data from no inputs.
/// Demonstrates the NoData input pattern for data generation nodes.
/// </summary>
/// <remarks>
/// <para>
/// This node generates 1000 random values sampled from a standard normal distribution
/// (mean = 0, standard deviation = 1) using the Box-Muller transform.
/// </para>
/// <para>
/// <strong>Purpose:</strong> Validates that nodes can operate without external inputs,
/// useful for testing, seeding, or synthetic data generation scenarios.
/// </para>
/// <para>
/// <strong>NoData Pattern:</strong> Input type is NoData, indicating this node doesn't
/// consume any external data sources. The Transform method ignores the input parameter.
/// </para>
/// </remarks>
public class GenerateSyntheticDataNode : NodeBase<NoData, SyntheticDataPoint> {
  // Hardcoded parameters for simplicity
  private const int SampleCount = 1000;
  private const double Mean = 0.0;
  private const double StdDev = 1.0;
  private const int RandomSeed = 42;

  protected override Task<IEnumerable<SyntheticDataPoint>> Transform(IEnumerable<NoData> input) {
    // Input is NoData - we ignore it and generate data from scratch
    var random = new Random(RandomSeed);
    var syntheticData = new List<SyntheticDataPoint>(SampleCount);

    // Generate samples using Box-Muller transform for normal distribution
    for (int i = 0; i < SampleCount; i++) {
      var value = GenerateNormalValue(random);
      syntheticData.Add(new SyntheticDataPoint { Index = i, Value = (float)value });
    }

    Logger?.LogInformation(
        "Generated {Count} synthetic data points from normal distribution (μ={Mean}, σ={StdDev})",
        SampleCount, Mean, StdDev);

    return Task.FromResult<IEnumerable<SyntheticDataPoint>>(syntheticData);
  }

  /// <summary>
  /// Generates a single value from a normal distribution using Box-Muller transform.
  /// </summary>
  /// <param name="random">Random number generator instance</param>
  /// <returns>Value sampled from N(μ, σ²) distribution</returns>
  private static double GenerateNormalValue(Random random) {
    // Box-Muller transform: converts uniform random values to normal distribution
    // u1, u2 ~ Uniform(0,1)
    var u1 = random.NextDouble();
    var u2 = random.NextDouble();

    // z0 ~ N(0,1)
    var z0 = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);

    // Scale and shift: z ~ N(μ, σ²)
    return Mean + StdDev * z0;
  }
}
