namespace Flowthru.Misc.ML.UMAP.Strategies.LocalMetric;

/// <summary>
/// Strategy interface for computing smooth approximations of local distances.
/// This is the second phase of the UMAP algorithm.
/// </summary>
/// <remarks>
/// <para>
/// The local metric phase computes bandwidth parameters (σᵢ and ρᵢ) for each point that
/// normalize the local neighborhood structure. This handles varying local densities in the data:
/// </para>
/// <list type="bullet">
///   <item><description><b>σᵢ (sigma)</b>: Bandwidth of the exponential kernel for point i</description></item>
///   <item><description><b>ρᵢ (rho)</b>: Distance to the nearest connected neighbor for point i</description></item>
/// </list>
/// <para>
/// These parameters ensure that each point has roughly the same "effective" number of neighbors
/// regardless of the local density, which is crucial for constructing a consistent fuzzy
/// simplicial set representation of the manifold.
/// </para>
/// <para>
/// <b>Mathematical goal:</b> Find σᵢ such that the fuzzy cardinality of the neighborhood equals k:
/// </para>
/// <code>
/// Σⱼ exp(-(dᵢⱼ - ρᵢ) / σᵢ) = log₂(k)
/// </code>
/// <para>
/// Python UMAP reference: <c>smooth_knn_dist()</c> function in <c>umap_.py</c> (lines ~143-250)
/// </para>
/// </remarks>
public interface ILocalMetricStrategy
{
  /// <summary>
  /// Computes smooth local metric parameters (bandwidths and local connectivity distances).
  /// </summary>
  /// <param name="knnDistances">
  /// Distance to k-nearest neighbors for each point.
  /// Array shape: (n_samples, n_neighbors)
  /// Each row should be sorted in ascending order.
  /// </param>
  /// <param name="k">
  /// Target number of effective neighbors (typically the same as n_neighbors).
  /// Used to calibrate the bandwidth parameter.
  /// </param>
  /// <param name="localConnectivity">
  /// Number of nearest neighbors that should be assumed to be connected at a local level.
  /// Typically 1.0, meaning the nearest neighbor is always assumed connected (distance weight = 1.0).
  /// Higher values (e.g., 2.0-5.0) increase local connectivity.
  /// Range: [1.0, k]
  /// </param>
  /// <param name="bandwidth">
  /// Target bandwidth multiplier for the exponential kernel.
  /// Default: 1.0. Affects the target cardinality (target = log₂(k) × bandwidth).
  /// </param>
  /// <returns>
  /// A result containing:
  /// - <b>Sigmas</b>: Bandwidth parameter for each point (length n_samples)
  /// - <b>Rhos</b>: Distance to nearest connected neighbor for each point (length n_samples)
  /// </returns>
  /// <remarks>
  /// <para>
  /// <b>Implementation requirements:</b>
  /// </para>
  /// <list type="number">
  ///   <item><description>Handle the case where points have fewer than k non-zero distances</description></item>
  ///   <item><description>Apply minimum distance scaling to prevent numerical instability</description></item>
  ///   <item><description>Ensure rho ≤ distance to k-th neighbor for all points</description></item>
  ///   <item><description>Thread-safe for parallel processing of points</description></item>
  /// </list>
  /// </remarks>
  LocalMetricResult ComputeLocalMetrics(
    float[][] knnDistances,
    float k,
    float localConnectivity = 1.0f,
    float bandwidth = 1.0f
  );
}

/// <summary>
/// Result of local metric computation.
/// </summary>
/// <param name="Sigmas">
/// Bandwidth parameters for exponential kernel.
/// Array shape: (n_samples,)
/// </param>
/// <param name="Rhos">
/// Distance to nearest connected neighbor.
/// Array shape: (n_samples,)
/// </param>
public sealed record LocalMetricResult(float[] Sigmas, float[] Rhos);
