using Flowthru.Extensions.ML.UMAP.Core.Markers;
using MathNet.Numerics.LinearAlgebra;

namespace Flowthru.Extensions.ML.UMAP.Strategies.NeighborSearch;

/// <summary>
/// Strategy interface for computing k-nearest neighbors in high-dimensional space.
/// This is the first phase of the UMAP algorithm.
/// </summary>
/// <typeparam name="TMetric">Phantom type indicating the distance metric this strategy uses.</typeparam>
/// <remarks>
/// <para>
/// The neighbor search phase computes the k-nearest neighbors for each point in the dataset.
/// Different strategies provide different trade-offs between accuracy, speed, and memory usage:
/// </para>
/// <list type="bullet">
///   <item><description><b>Exact methods</b> (e.g., brute force): O(n²) time, 100% accurate, recommended for datasets &lt; 4096 samples</description></item>
///   <item><description><b>Tree methods</b> (e.g., KD-tree): O(n log n) time, exact or approximate, suitable for medium datasets with low-to-medium dimensions</description></item>
///   <item><description><b>Approximate methods</b> (e.g., NN-Descent): O(n^1.14) time, ~99% accurate, recommended for datasets ≥ 4096 samples</description></item>
///   <item><description><b>Precomputed</b>: O(1) time, user provides k-NN graph, suitable when neighbors are already known</description></item>
/// </list>
/// <para>
/// Python UMAP reference: <c>nearest_neighbors()</c> function in <c>umap_.py</c> (lines ~260-300)
/// </para>
/// </remarks>
public interface INeighborSearchStrategy<TMetric>
  where TMetric : IMetricMarker
{
  /// <summary>
  /// Computes k-nearest neighbors for all points in the dataset.
  /// </summary>
  /// <param name="data">
  /// Input data matrix where each row represents a data point and each column a feature.
  /// Shape: (n_samples, n_features)
  /// </param>
  /// <param name="nNeighbors">
  /// Number of nearest neighbors to find for each point.
  /// Must be at least 2 and at most n_samples - 1.
  /// </param>
  /// <param name="metric">
  /// Distance metric function that computes the distance between two points.
  /// Should be compatible with the TMetric phantom type.
  /// </param>
  /// <param name="random">
  /// Random number generator for any randomized algorithms (e.g., approximate search).
  /// Ensures reproducibility when a seed is provided.
  /// </param>
  /// <returns>
  /// A result containing:
  /// - <b>Indices</b>: n_samples × n_neighbors array where Indices[i][j] is the index of the j-th nearest neighbor of point i
  /// - <b>Distances</b>: n_samples × n_neighbors array where Distances[i][j] is the distance to that neighbor
  /// - <b>SearchIndex</b>: Optional search index structure for future queries (e.g., for transform), or null if not applicable
  ///
  /// Note: Indices[i][0] should always be i (each point is its own nearest neighbor with distance 0).
  /// </returns>
  /// <remarks>
  /// <para>
  /// <b>Implementation requirements:</b>
  /// </para>
  /// <list type="number">
  ///   <item><description>Results must be sorted by distance (ascending) for each point</description></item>
  ///   <item><description>First neighbor of each point should typically be itself (distance 0)</description></item>
  ///   <item><description>For precomputed distances with disconnected components, use index -1 and distance ∞</description></item>
  ///   <item><description>Thread-safe if marked as such in implementation</description></item>
  /// </list>
  /// </remarks>
  NeighborSearchResult Search(
    Matrix<float> data,
    int nNeighbors,
    Func<ReadOnlySpan<float>, ReadOnlySpan<float>, float> metric,
    Random random
  );
}

/// <summary>
/// Result of a nearest neighbor search operation.
/// </summary>
/// <param name="Indices">
/// Indices of k-nearest neighbors for each point.
/// Array shape: (n_samples, n_neighbors)
/// </param>
/// <param name="Distances">
/// Distances to k-nearest neighbors for each point.
/// Array shape: (n_samples, n_neighbors)
/// </param>
/// <param name="SearchIndex">
/// Optional search index for future queries (used in transform operations).
/// May be null if the strategy doesn't support indexing.
/// </param>
public sealed record NeighborSearchResult(
  int[][] Indices,
  float[][] Distances,
  object? SearchIndex
);
