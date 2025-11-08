using MathNet.Numerics.LinearAlgebra;

namespace Flowthru.Extensions.MLPure.UMAP.Algorithms;

/// <summary>
/// K-Nearest Neighbors computation - pure Python port.
/// Based on umap/umap_.py: nearest_neighbors function.
/// This uses brute-force exact k-NN (no approximation).
/// </summary>
public static class NearestNeighbors
{
  /// <summary>
  /// Compute k-nearest neighbors using brute-force search.
  /// Python reference: umap_.py lines ~260-300 (nearest_neighbors function)
  /// </summary>
  public static (int[][] Indices, float[][] Distances) ComputeKnn(
    Matrix<float> data,
    int nNeighbors,
    Func<ReadOnlySpan<float>, ReadOnlySpan<float>, float> metric
  )
  {
    int nSamples = data.RowCount;
    var indices = new int[nSamples][];
    var distances = new float[nSamples][];

    // Convert to row arrays for faster access
    var dataRows = new float[nSamples][];
    for (int i = 0; i < nSamples; i++)
    {
      dataRows[i] = data.Row(i).ToArray();
    }

    // Compute k-NN for each point (pure brute-force)
    for (int i = 0; i < nSamples; i++)
    {
      // Compute distances to all other points
      var distList = new List<(int index, float distance)>(nSamples);

      for (int j = 0; j < nSamples; j++)
      {
        float dist = metric(dataRows[i], dataRows[j]);
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

    return (indices, distances);
  }
}
