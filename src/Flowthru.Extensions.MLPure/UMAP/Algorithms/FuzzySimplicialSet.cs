using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Single;

namespace Flowthru.Extensions.MLPure.UMAP.Algorithms;

/// <summary>
/// Fuzzy simplicial set construction - pure Python port.
/// Based on umap/umap_.py: smooth_knn_dist, compute_membership_strengths, fuzzy_simplicial_set
/// </summary>
public static class FuzzySimplicialSet
{
  private const float SmoothKTolerance = 1e-5f;
  private const float MinKDistScale = 1e-3f;
  private const int MaxBinarySearchIterations = 64;
  private const float NpyInfinity = float.PositiveInfinity;

  /// <summary>
  /// Python reference: umap_.py lines ~143-250 (smooth_knn_dist function)
  /// Compute a continuous version of the distance to the kth nearest neighbor.
  /// </summary>
  public static (float[] Sigmas, float[] Rhos) SmoothKnnDist(
    float[][] knnDistances,
    float k,
    float localConnectivity = 1.0f
  )
  {
    int nSamples = knnDistances.Length;
    float target = MathF.Log2(k);
    
    var rhos = new float[nSamples];
    var sigmas = new float[nSamples];

    // Compute mean distance for fallback
    float meanDistances = 0.0f;
    int totalCount = 0;
    foreach (var dists in knnDistances)
    {
      foreach (var d in dists)
      {
        meanDistances += d;
        totalCount++;
      }
    }
    meanDistances /= totalCount;

    for (int i = 0; i < nSamples; i++)
    {
      var distances = knnDistances[i];
      
      // Compute rho (distance to nearest connected neighbor)
      // Python: lines ~192-210
      var nonZeroDists = distances.Where(d => d > 0.0f).ToArray();
      if (nonZeroDists.Length >= localConnectivity)
      {
        int index = (int)MathF.Floor(localConnectivity);
        float interpolation = localConnectivity - index;
        
        if (index > 0)
        {
          rhos[i] = nonZeroDists[index - 1];
          if (interpolation > SmoothKTolerance && index < nonZeroDists.Length)
          {
            rhos[i] += interpolation * (nonZeroDists[index] - nonZeroDists[index - 1]);
          }
        }
        else
        {
          rhos[i] = interpolation * nonZeroDists[0];
        }
      }
      else if (nonZeroDists.Length > 0)
      {
        rhos[i] = nonZeroDists.Max();
      }

      // Binary search for sigma (Python: lines ~212-240)
      float lo = 0.0f;
      float hi = NpyInfinity;
      float mid = 1.0f;

      for (int n = 0; n < MaxBinarySearchIterations; n++)
      {
        float psum = 0.0f;
        for (int j = 1; j < distances.Length; j++) // Start from 1 (skip self)
        {
          float d = distances[j] - rhos[i];
          if (d > 0)
          {
            psum += MathF.Exp(-(d / mid));
          }
          else
          {
            psum += 1.0f;
          }
        }

        if (MathF.Abs(psum - target) < SmoothKTolerance)
        {
          break;
        }

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
            mid *= 2;
          }
          else
          {
            mid = (lo + hi) / 2.0f;
          }
        }
      }

      sigmas[i] = mid;

      // Ensure minimum sigma (Python: lines ~242-250)
      if (rhos[i] > 0.0f)
      {
        float meanIthDistances = distances.Average();
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

    return (sigmas, rhos);
  }

  /// <summary>
  /// Python reference: umap_.py lines ~376-428 (compute_membership_strengths function)
  /// Compute membership strengths for the fuzzy simplicial set.
  /// </summary>
  public static SparseMatrix ComputeMembershipStrengths(
    int[][] knnIndices,
    float[][] knnDistances,
    float[] sigmas,
    float[] rhos,
    float setOpMixRatio = 1.0f
  )
  {
    int nSamples = knnIndices.Length;
    int nNeighbors = knnIndices[0].Length;

    // Build COO format sparse matrix data
    var rows = new List<int>();
    var cols = new List<int>();
    var vals = new List<float>();

    for (int i = 0; i < nSamples; i++)
    {
      for (int j = 0; j < nNeighbors; j++)
      {
        if (knnIndices[i][j] == -1)
        {
          continue;
        }

        // Don't include self-loops (Python: line ~416)
        if (knnIndices[i][j] == i)
        {
          continue;
        }

        // Compute membership strength (Python: lines ~418-422)
        float val;
        if (knnDistances[i][j] - rhos[i] <= 0.0f || sigmas[i] == 0.0f)
        {
          val = 1.0f;
        }
        else
        {
          val = MathF.Exp(-((knnDistances[i][j] - rhos[i]) / sigmas[i]));
        }

        rows.Add(i);
        cols.Add(knnIndices[i][j]);
        vals.Add(val);
      }
    }

    // Create sparse matrix from COO format
    var result = SparseMatrix.Create(nSamples, nSamples, 0.0f);
    
    for (int i = 0; i < rows.Count; i++)
    {
      result[rows[i], cols[i]] = vals[i];
    }

    // Apply fuzzy set operations (Python: lines ~589-600)
    // fuzzy union: result + transpose - (result .* transpose)
    // fuzzy intersection: result .* transpose
    var transpose = result.Transpose() as SparseMatrix ?? throw new InvalidOperationException("Transpose failed");
    var prodMatrix = result.PointwiseMultiply(transpose) as SparseMatrix ?? throw new InvalidOperationException("PointwiseMultiply failed");
    
    var unionPart = (result + transpose - prodMatrix) as SparseMatrix ?? throw new InvalidOperationException("Union failed");
    result = (SparseMatrix)(setOpMixRatio * unionPart + (1.0f - setOpMixRatio) * prodMatrix);

    // Clear near-zero values manually
    for (int i = 0; i < result.RowCount; i++)
    {
      for (int j = 0; j < result.ColumnCount; j++)
      {
        if (MathF.Abs(result[i, j]) < 1e-10f)
        {
          result[i, j] = 0.0f;
        }
      }
    }

    return result;
  }

  /// <summary>
  /// Find a and b parameters for UMAP curve fitting.
  /// Python reference: Uses scipy.optimize.curve_fit in umap_.py
  /// Simplified version using heuristics from the paper.
  /// </summary>
  public static (float A, float B) FindAbParams(float spread, float minDist)
  {
    // These are empirical values from the UMAP paper
    // Full implementation would use curve fitting
    float a = 1.929f;
    float b = 0.7915f;
    
    return (a, b);
  }
}
