using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Single;

namespace Flowthru.Extensions.MLPure.UMAP.Algorithms;

/// <summary>
/// Layout optimization for UMAP - pure Python port.
/// Based on umap/layouts.py: optimize_layout_euclidean
/// </summary>
public static class Layout
{
  /// <summary>
  /// Optimize the embedding using stochastic gradient descent.
  /// Python reference: layouts.py: optimize_layout_euclidean function (lines ~600-800)
  /// </summary>
  public static Matrix<float> OptimizeLayoutEuclidean(
    Matrix<float> embedding,
    SparseMatrix graph,
    int nEpochs,
    float initialAlpha,
    float a,
    float b,
    float gamma, // repulsion_strength
    int negativeSampleRate,
    Random random,
    IProgress<(string, float, string?)>? progress = null
  )
  {
    int nSamples = embedding.RowCount;
    int nComponents = embedding.ColumnCount;

    // Extract edges from graph
    var edges = new List<(int source, int target, float weight)>();
    foreach (var entry in graph.EnumerateIndexed())
    {
      if (entry.Item3 > 0.0f)
      {
        edges.Add((entry.Item1, entry.Item2, entry.Item3));
      }
    }

    int nEdges = edges.Count;

    // Compute epochs_per_sample (Python: make_epochs_per_sample function, lines ~914-930)
    var epochsPerSample = MakeEpochsPerSample(edges.Select(e => e.weight).ToArray(), nEpochs);
    var epochOfNextSample = epochsPerSample.ToArray(); // Track when each edge should be sampled

    // SGD optimization loop (Python: lines ~650-750)
    for (int epoch = 0; epoch < nEpochs; epoch++)
    {
      // Decay learning rate
      float alpha = initialAlpha * (1.0f - ((float)epoch / nEpochs));

      // Process each edge
      for (int edgeIdx = 0; edgeIdx < nEdges; edgeIdx++)
      {
        if (epochOfNextSample[edgeIdx] > epoch)
        {
          continue;
        }

        var edge = edges[edgeIdx];
        int j = edge.source;
        int k = edge.target;

        // Get current embedding positions
        var current = embedding.Row(j).ToArray();
        var other = embedding.Row(k).ToArray();

        // Compute distance squared
        float distSquared = Utils.RDist(current, other);

        // Compute attractive force gradient (Python: lines ~96-150 in layouts.py)
        float gradCoeff;
        if (distSquared > 0.0f)
        {
          gradCoeff = -2.0f * a * b * MathF.Pow(distSquared, b - 1.0f);
          gradCoeff /= a * MathF.Pow(distSquared, b) + 1.0f;
        }
        else
        {
          gradCoeff = 0.0f;
        }

        // Apply attractive gradient
        for (int d = 0; d < nComponents; d++)
        {
          float gradD = Utils.Clip(gradCoeff * (current[d] - other[d]));
          embedding[j, d] += gradD * alpha;
          embedding[k, d] += -gradD * alpha;
        }

        // Negative sampling (Python: lines ~152-180)
        for (int p = 0; p < negativeSampleRate; p++)
        {
          int negK = random.Next(nSamples);
          if (negK == j)
          {
            continue;
          }

          var negOther = embedding.Row(negK).ToArray();
          float negDistSquared = Utils.RDist(current, negOther);

          // Compute repulsive force gradient
          float negGradCoeff;
          if (negDistSquared > 0.0f)
          {
            negGradCoeff = 2.0f * gamma * b;
            negGradCoeff /= (0.001f + negDistSquared) * (a * MathF.Pow(negDistSquared, b) + 1.0f);
          }
          else
          {
            negGradCoeff = 0.0f;
          }

          // Apply repulsive gradient
          for (int d = 0; d < nComponents; d++)
          {
            float negGradD = Utils.Clip(negGradCoeff * (current[d] - negOther[d]));
            embedding[j, d] += negGradD * alpha;
          }
        }

        // Update next sample epoch for this edge
        epochOfNextSample[edgeIdx] += epochsPerSample[edgeIdx];
      }

      // Report progress
      if (progress != null && epoch % 10 == 0)
      {
        progress.Report(("Optimizing", (float)epoch / nEpochs, $"Epoch {epoch}/{nEpochs}"));
      }
    }

    return embedding;
  }

  /// <summary>
  /// Compute epochs per sample for each edge.
  /// Python reference: umap_.py lines ~914-930 (make_epochs_per_sample function)
  /// </summary>
  private static float[] MakeEpochsPerSample(float[] weights, int nEpochs)
  {
    var result = new float[weights.Length];
    float maxWeight = weights.Max();

    for (int i = 0; i < weights.Length; i++)
    {
      float nSamples = nEpochs * (weights[i] / maxWeight);
      if (nSamples > 0)
      {
        result[i] = (float)nEpochs / nSamples;
      }
      else
      {
        result[i] = -1.0f;
      }
    }

    return result;
  }
}
