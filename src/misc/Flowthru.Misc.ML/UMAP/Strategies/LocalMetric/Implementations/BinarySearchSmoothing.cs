using Flowthru.Misc.ML.UMAP.Strategies.LocalMetric;

namespace Flowthru.Misc.ML.UMAP.Strategies.LocalMetric.Implementations;

/// <summary>
/// Binary search-based local metric smoothing.
/// Computes bandwidth parameters using iterative binary search to match target cardinality.
/// </summary>
/// <remarks>
/// <para>
/// This is the standard UMAP approach for computing local metric parameters. For each point,
/// it uses binary search to find the bandwidth σ that makes the fuzzy cardinality of its
/// neighborhood equal to the target value (log₂(k)).
/// </para>
/// <para>
/// <b>Algorithm:</b>
/// </para>
/// <list type="number">
///   <item><description>Compute ρᵢ (distance to nearest connected neighbor) based on local connectivity</description></item>
///   <item><description>Use binary search to find σᵢ such that Σⱼ exp(-(dᵢⱼ - ρᵢ)/σᵢ) ≈ log₂(k)</description></item>
///   <item><description>Apply minimum distance scaling to prevent numerical instability</description></item>
/// </list>
/// <para>
/// <b>Characteristics:</b>
/// </para>
/// <list type="bullet">
///   <item><description><b>Time complexity</b>: O(n × k × log(max_iter)) ≈ O(n × k)</description></item>
///   <item><description><b>Space complexity</b>: O(n) for output</description></item>
///   <item><description><b>Convergence</b>: Typically within 10-20 iterations per point</description></item>
///   <item><description><b>Thread-safe</b>: Yes (each point computed independently)</description></item>
/// </list>
/// <para>
/// Python reference: <c>smooth_knn_dist()</c> function in <c>umap_.py</c> (lines ~143-250).
/// This is a direct port of the numba-jitted Python implementation.
/// </para>
/// </remarks>
public sealed class BinarySearchSmoothing : ILocalMetricStrategy
{
  private const float SmoothKTolerance = 1e-5f;
  private const float MinKDistScale = 1e-3f;
  private const float NpyInfinity = float.PositiveInfinity;

  /// <summary>
  /// Maximum number of binary search iterations per point.
  /// Typically converges much faster, but this provides a safety limit.
  /// </summary>
  public int MaxIterations { get; init; } = 64;

  /// <inheritdoc />
  public LocalMetricResult ComputeLocalMetrics(
    float[][] knnDistances,
    float k,
    float localConnectivity = 1.0f,
    float bandwidth = 1.0f
  )
  {
    int nSamples = knnDistances.Length;
    float target = MathF.Log2(k) * bandwidth;

    var rhos = new float[nSamples];
    var sigmas = new float[nSamples];

    // Compute mean distance for fallback scaling
    float meanDistances = ComputeMeanDistance(knnDistances);

    // Process each point independently (parallelizable)
    for (int i = 0; i < nSamples; i++)
    {
      var distances = knnDistances[i];

      // Compute rho: distance to nearest connected neighbor
      // Based on local_connectivity parameter
      rhos[i] = ComputeRho(distances, localConnectivity);

      // Binary search for sigma
      sigmas[i] = BinarySearchForSigma(distances, rhos[i], target);

      // Apply minimum distance scaling to prevent numerical issues
      float meanIthDistances = ComputeMean(distances);
      if (rhos[i] > 0.0f)
      {
        if (sigmas[i] < MinKDistScale * meanIthDistances)
        {
          sigmas[i] = MinKDistScale * meanIthDistances;
        }
      }
      else
      {
        if (sigmas[i] < MinKDistScale * meanDistances)
        {
          sigmas[i] = MinKDistScale * meanDistances;
        }
      }
    }

    return new LocalMetricResult(sigmas, rhos);
  }

  /// <summary>
  /// Computes rho (distance to nearest connected neighbor) based on local connectivity.
  /// </summary>
  private static float ComputeRho(float[] distances, float localConnectivity)
  {
    // Get non-zero distances (exclude self-distance at index 0)
    var nonZeroDists = distances.Where(d => d > 0.0f).ToArray();

    if (nonZeroDists.Length == 0)
      return 0.0f;

    if (nonZeroDists.Length < localConnectivity)
      return nonZeroDists.Max();

    // Interpolate based on local_connectivity
    int index = (int)MathF.Floor(localConnectivity);
    float interpolation = localConnectivity - index;

    if (index > 0 && index <= nonZeroDists.Length)
    {
      float rho = nonZeroDists[index - 1];
      if (interpolation > SmoothKTolerance && index < nonZeroDists.Length)
      {
        rho += interpolation * (nonZeroDists[index] - nonZeroDists[index - 1]);
      }
      return rho;
    }

    return interpolation * nonZeroDists[0];
  }

  /// <summary>
  /// Binary search to find sigma such that the fuzzy cardinality equals the target.
  /// </summary>
  private float BinarySearchForSigma(float[] distances, float rho, float target)
  {
    float lo = 0.0f;
    float hi = NpyInfinity;
    float mid = 1.0f;

    for (int iteration = 0; iteration < MaxIterations; iteration++)
    {
      // Compute current cardinality with this sigma
      float psum = 0.0f;
      for (int j = 1; j < distances.Length; j++) // Skip self (j=0)
      {
        float d = distances[j] - rho;
        if (d > 0)
        {
          psum += MathF.Exp(-(d / mid));
        }
        else
        {
          psum += 1.0f;
        }
      }

      // Check convergence
      if (MathF.Abs(psum - target) < SmoothKTolerance)
      {
        break;
      }

      // Binary search update
      if (psum > target)
      {
        hi = mid;
        mid = (lo + hi) / 2.0f;
      }
      else
      {
        lo = mid;
        if (hi == NpyInfinity)
        {
          mid *= 2.0f;
        }
        else
        {
          mid = (lo + hi) / 2.0f;
        }
      }
    }

    return mid;
  }

  private static float ComputeMeanDistance(float[][] distances)
  {
    float sum = 0.0f;
    int count = 0;
    foreach (var dists in distances)
    {
      foreach (var d in dists)
      {
        sum += d;
        count++;
      }
    }
    return count > 0 ? sum / count : 0.0f;
  }

  private static float ComputeMean(float[] values)
  {
    if (values.Length == 0)
      return 0.0f;
    return values.Sum() / values.Length;
  }
}
