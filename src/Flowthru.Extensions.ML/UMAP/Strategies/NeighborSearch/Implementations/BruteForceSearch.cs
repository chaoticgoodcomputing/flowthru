using Flowthru.Extensions.ML.UMAP.Core.Markers;
using Flowthru.Extensions.ML.UMAP.Strategies.NeighborSearch;

namespace Flowthru.Extensions.ML.UMAP.Strategies.NeighborSearch.Implementations;

/// <summary>
/// Brute-force exact k-nearest neighbor search.
/// Computes all pairwise distances - O(n²) time complexity.
/// </summary>
/// <typeparam name="TMetric">Distance metric marker type.</typeparam>
/// <remarks>
/// <para>
/// This implementation computes the exact k-nearest neighbors by calculating all pairwise
/// distances and selecting the k smallest for each point. While this is computationally
/// expensive for large datasets, it guarantees 100% accuracy and is the fastest approach
/// for small datasets (typically &lt; 4096 samples).
/// </para>
/// <para>
/// <b>Characteristics:</b>
/// </para>
/// <list type="bullet">
///   <item><description><b>Time complexity</b>: O(n² × d) where n=samples, d=dimensions</description></item>
///   <item><description><b>Space complexity</b>: O(n × k) for output</description></item>
///   <item><description><b>Accuracy</b>: 100% (exact)</description></item>
///   <item><description><b>Recommended for</b>: Small datasets (&lt; 4096 samples)</description></item>
///   <item><description><b>Thread-safe</b>: Yes (read-only operations)</description></item>
/// </list>
/// <para>
/// This is the reference implementation matching Python UMAP's behavior for small datasets
/// or when <c>metric='precomputed'</c> is not used.
/// </para>
/// <para>
/// Python reference: The brute-force path in <c>nearest_neighbors()</c> when exact k-NN
/// is computed via <c>pairwise_distances()</c> (Python UMAP lines ~2950-3000).
/// </para>
/// </remarks>
public sealed class BruteForceSearch<TMetric> : INeighborSearchStrategy<TMetric>
  where TMetric : IMetricMarker
{
  /// <inheritdoc />
  public NeighborSearchResult Search(
    float[][] data,
    int nNeighbors,
    Func<ReadOnlySpan<float>, ReadOnlySpan<float>, float> metric,
    Random random
  )
  {
    int nSamples = data.Length;

    if (nNeighbors > nSamples)
    {
      throw new ArgumentException(
        $"Cannot find {nNeighbors} neighbors with only {nSamples} samples",
        nameof(nNeighbors)
      );
    }

    var indices = new int[nSamples][];
    var distances = new float[nSamples][];

    // Data is already in row format (jagged array), no conversion needed
    // Compute k-NN for each point using brute-force
    for (int i = 0; i < nSamples; i++)
    {
      // Compute distances to all other points
      var distList = new List<(int index, float distance)>(nSamples);

      for (int j = 0; j < nSamples; j++)
      {
        float dist = metric(data[i].AsSpan(), data[j].AsSpan());
        distList.Add((j, dist));
      }

      // Sort by distance and take top k
      distList.Sort((a, b) => a.distance.CompareTo(b.distance));

      indices[i] = new int[nNeighbors];
      distances[i] = new float[nNeighbors];

      for (int k = 0; k < nNeighbors; k++)
      {
        indices[i][k] = distList[k].index;
        distances[i][k] = distList[k].distance;
      }
    }

    // Brute-force search doesn't produce a reusable index
    return new NeighborSearchResult(indices, distances, SearchIndex: null);
  }
}
