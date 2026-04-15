using System;
using System.Collections.Generic;
using Flowthru.Misc.ML.UMAP.Core.Markers;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using MathNet.Numerics.LinearAlgebra.Single;

namespace Flowthru.Misc.ML.UMAP.Strategies.LayoutInit.Implementations;

/// <summary>
/// Spectral initialization via eigendecomposition of the graph Laplacian.
/// Produces a high-quality initialization for connected graphs on small-to-medium datasets.
/// </summary>
public sealed class SpectralInit : ILayoutInitStrategy
{
    private const float NoiseScaleFactor = 0.0001f;
    private const float MaxCoord = 10.0f;

    /// <summary>
    /// Initializes the layout using spectral embedding. This involves computing the eigenvectors of the graph Laplacian and using them as the initial coordinates for the embedding. The resulting layout is then normalized and small noise is added to help with optimization convergence.
    /// Spectral initialization can provide a better starting point for UMAP optimization, especially for connected graphs, leading to faster convergence and improved embedding quality compared to random initialization. However, it can be computationally expensive for large datasets due to the eigendecomposition step, so it is typically recommended for small-to-medium datasets (e.g., up to a few thousand samples).
    /// If an exception occurs during spectral embedding (e.g., due to numerical issues), the method falls back to random initialization to ensure robustness.
    ///
    /// </summary>
    /// <param name="data"></param>
    /// <param name="graph"></param>
    /// <param name="nComponents"></param>
    /// <param name="random"></param>
    /// <returns></returns>
    public LayoutInitResult InitializeLayout(
      Matrix<float>? data,
      MathNet.Numerics.LinearAlgebra.Single.SparseMatrix graph,
      int nComponents,
      Random random
    )
    {
        Console.WriteLine(
          $"[SpectralInit] Starting layout initialization (n={graph.RowCount}, nComponents={nComponents})"
        );

        ValidateInputs(graph, nComponents);

        Console.WriteLine($"[SpectralInit] Validation passed, computing spectral embedding...");

        try
        {
            var embedding = ComputeSpectralEmbedding(graph, nComponents);

            Console.WriteLine($"[SpectralInit] Spectral embedding computed, normalizing...");

            embedding = NormalizeAndNoisify(embedding, random);

            Console.WriteLine($"[SpectralInit] Layout initialization completed successfully");

            return new LayoutInitResult(embedding, "spectral");
        }
        catch (Exception ex)
        {
            Console.WriteLine(
              $"[SpectralInit] Exception during spectral embedding: {ex.Message}, falling back to random"
            );

            // Fallback to random initialization for robustness
            var fallback = RandomFallback(graph.RowCount, nComponents, random);
            return new LayoutInitResult(fallback, "spectral-fallback-random");
        }
    }

    private static void ValidateInputs(
      MathNet.Numerics.LinearAlgebra.Single.SparseMatrix graph,
      int nComponents
    )
    {
        if (nComponents < 1)
        {
            throw new ArgumentException(
              $"nComponents must be >= 1 (got {nComponents})",
              nameof(nComponents)
            );
        }

        if (nComponents >= graph.RowCount)
        {
            throw new ArgumentException(
              $"nComponents ({nComponents}) must be less than number of samples ({graph.RowCount})",
              nameof(nComponents)
            );
        }
    }

    /// <summary>
    /// Compute spectral embedding: take the smallest non-zero eigenvectors of the Laplacian.
    /// </summary>
    private static Matrix<float> ComputeSpectralEmbedding(
      MathNet.Numerics.LinearAlgebra.Single.SparseMatrix graph,
      int nComponents
    )
    {
        Console.WriteLine(
          $"[SpectralInit.ComputeSpectralEmbedding] Building dense adjacency matrix (n={graph.RowCount})..."
        );

        // Build double-precision adjacency matrix for robust eigendecomposition
        int n = graph.RowCount;
        var adjD = MathNet.Numerics.LinearAlgebra.Double.DenseMatrix.Create(n, n, 0.0);

        foreach (var item in graph.EnumerateIndexed())
        {
            var i = item.Item1;
            var j = item.Item2;
            var v = item.Item3;
            adjD[i, j] = v;
        }

        Console.WriteLine($"[SpectralInit.ComputeSpectralEmbedding] Building degree matrix...");

        // Degree matrix (double)
        var degreeD = MathNet.Numerics.LinearAlgebra.Double.DenseMatrix.CreateDiagonal(
          n,
          n,
          i =>
          {
              return adjD.Row(i).Sum();
          }
        );

        Console.WriteLine($"[SpectralInit.ComputeSpectralEmbedding] Computing Laplacian...");

        // Unnormalized Laplacian L = D - A
        var lap = degreeD - adjD;

        Console.WriteLine(
          $"[SpectralInit.ComputeSpectralEmbedding] Computing eigendecomposition (this may take a while for n={n})..."
        );

        // Use MathNet's Evd on symmetric matrix (double)
        var evd = lap.Evd();

        Console.WriteLine(
          $"[SpectralInit.ComputeSpectralEmbedding] Eigendecomposition complete, sorting eigenpairs..."
        );

        // Collect eigenpairs and sort by eigenvalue ascending
        var eigenPairs =
          new List<(double value, MathNet.Numerics.LinearAlgebra.Vector<double> vector)>();
        for (var idx = 0; idx < evd.EigenValues.Count; idx++)
        {
            var val = evd.EigenValues[idx].Real;
            var vec = evd.EigenVectors.Column(idx);
            eigenPairs.Add((val, vec));
        }

        eigenPairs.Sort((a, b) => a.value.CompareTo(b.value));

        // Skip the first eigenvector (near-zero eigenvalue)
        var start = 1;
        var embedding = MathNet.Numerics.LinearAlgebra.Single.DenseMatrix.Create(n, nComponents, 0.0f);
        for (var c = 0; c < nComponents; c++)
        {
            var vecD = eigenPairs[start + c].vector;
            for (var r = 0; r < n; r++)
            {
                embedding[r, c] = (float)vecD[r];
            }
        }

        return embedding;
    }

    /// <summary>
    /// Normalize to [-MaxCoord, MaxCoord] and add tiny noise proportional to nearest-neighbor distance.
    /// </summary>
    private static Matrix<float> NormalizeAndNoisify(Matrix<float> embedding, Random random)
    {
        var n = embedding.RowCount;
        var d = embedding.ColumnCount;

        var min = new float[d];
        var max = new float[d];
        for (var j = 0; j < d; j++)
        {
            min[j] = float.MaxValue;
            max[j] = float.MinValue;
            for (var i = 0; i < n; i++)
            {
                var v = embedding[i, j];
                if (v < min[j])
                {
                    min[j] = v;
                }
                if (v > max[j])
                {
                    max[j] = v;
                }
            }
        }

        // Scale to [-MaxCoord, MaxCoord]
        for (var j = 0; j < d; j++)
        {
            var range = max[j] - min[j];
            if (range == 0)
            {
                range = 1.0f;
            }
            for (var i = 0; i < n; i++)
            {
                embedding[i, j] = MaxCoord * (2.0f * ((embedding[i, j] - min[j]) / range) - 1.0f);
            }
        }

        // Add tiny noise
        var nndistNoiseScale = 0.001f;
        for (var i = 0; i < n; i++)
        {
            for (var j = 0; j < d; j++)
            {
                embedding[i, j] += SampleNormal(random, 0.0f, nndistNoiseScale * NoiseScaleFactor);
            }
        }

        return embedding;
    }

    private static Matrix<float> RandomFallback(int nSamples, int nComponents, Random random)
    {
        var mat = MathNet.Numerics.LinearAlgebra.Single.DenseMatrix.Create(nSamples, nComponents, 0.0f);
        for (var i = 0; i < nSamples; i++)
        {
            for (var j = 0; j < nComponents; j++)
            {
                mat[i, j] = (float)(random.NextDouble() * 2.0 - 1.0) * MaxCoord;
            }
        }

        return mat;
    }

    private static float SampleNormal(Random rng, float mean, float std)
    {
        // Box-Muller transform
        var u1 = 1.0 - rng.NextDouble();
        var u2 = 1.0 - rng.NextDouble();
        var randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
        return (float)(mean + std * randStdNormal);
    }
}
