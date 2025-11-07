using System.Diagnostics;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Single;

namespace Flowthru.Extensions.ML.UMAP.Algorithms;

/// <summary>
/// Fuzzy simplicial set construction for UMAP.
/// </summary>
/// <remarks>
/// Based on the UMAP Python implementation by Leland McInnes.
/// Constructs a fuzzy topological representation of the data.
/// Reference: https://github.com/lmcinnes/umap
/// </remarks>
public static class FuzzySimplicialSet
{
  private const float SmoothKTolerance = 1e-5f;
  private const float MinKDistScale = 1e-3f;
  private const int MaxNewtonIterations = 10;

  /// <summary>
  /// Computes rho (distance to nearest neighbor with local connectivity).
  /// </summary>
  private static float ComputeRho(float[] distances, float localConnectivity)
  {
    if (localConnectivity <= 0)
    {
      return 0f;
    }

    int index = (int)MathF.Floor(localConnectivity);
    float interpolation = localConnectivity - index;

    if (index <= 0)
    {
      return 0f;
    }

    float rho = distances[index];
    if (index < distances.Length - 1)
    {
      rho = (1f - interpolation) * distances[index] + interpolation * distances[index + 1];
    }

    return rho;
  }

  /// <summary>
  /// Computes sigma using Newton's method for faster convergence.
  /// </summary>
  /// <remarks>
  /// Newton's method typically converges in 5-10 iterations vs 64 for binary search.
  /// We're solving: Σexp(-d/σ) = target for σ.
  /// </remarks>
  private static float ComputeSigmaNewton(float[] distances, float rho, float target)
  {
    // Better initial guess based on mean distance
    float meanDist = 0f;
    int validCount = 0;
    for (int j = 1; j < distances.Length; j++)
    {
      float d = Math.Max(0f, distances[j] - rho);
      if (d > 0f)
      {
        meanDist += d;
        validCount++;
      }
    }

    if (validCount == 0)
    {
      return 1f;
    }

    meanDist /= validCount;

    // Initial guess: meanDist / ln(validCount)
    float sigma = meanDist / MathF.Log(validCount + 1f);
    sigma = Math.Max(MinKDistScale, sigma);

    // Newton's method: σ_new = σ - f(σ)/f'(σ)
    // where f(σ) = Σexp(-d/σ) - target
    // and f'(σ) = Σ(d/σ²)exp(-d/σ)
    for (int iter = 0; iter < MaxNewtonIterations; iter++)
    {
      float psum = 0f; // f(σ)
      float dpsum = 0f; // f'(σ)

      for (int j = 1; j < distances.Length; j++)
      {
        float d = Math.Max(0f, distances[j] - rho);
        if (d > 0f)
        {
          float expTerm = MathF.Exp(-d / sigma);
          psum += expTerm;
          dpsum += (d / (sigma * sigma)) * expTerm;
        }
      }

      float error = psum - target;

      // Check convergence
      if (MathF.Abs(error) < SmoothKTolerance)
      {
        break;
      }

      // Avoid division by zero
      if (MathF.Abs(dpsum) < 1e-10f)
      {
        break;
      }

      // Newton update
      float newSigma = sigma - error / dpsum;

      // Ensure sigma stays positive and reasonable
      newSigma = Math.Max(MinKDistScale, newSigma);

      // Check for stagnation
      if (MathF.Abs(newSigma - sigma) < MinKDistScale)
      {
        break;
      }

      sigma = newSigma;
    }

    return sigma;
  }

  /// <summary>
  /// Computes smooth k-nearest neighbor parameters for a single sample.
  /// </summary>
  private static (float Sigma, float Rho) ComputeSmoothKnnForSample(
    float[] distances,
    float localConnectivity,
    float target
  )
  {
    float rho = ComputeRho(distances, localConnectivity);
    float sigma = ComputeSigmaNewton(distances, rho, target);
    return (sigma, rho);
  }

  /// <summary>
  /// Creates a progress reporter for smooth k-NN computation.
  /// </summary>
  private static Action<int> CreateSmoothKnnProgressReporter(
    int nSamples,
    Stopwatch stopwatch,
    int verbosity,
    IProgress<(string Stage, float Progress, string? Details)>? progressReporter,
    object lockObj
  )
  {
    int reportInterval = Math.Max(1, nSamples / 20);

    return (int count) =>
    {
      if (verbosity >= 2 && count % reportInterval == 0)
      {
        float progress = (float)count / nSamples;
        double elapsed = stopwatch.Elapsed.TotalSeconds;
        double rate = count / elapsed;
        double eta = (nSamples - count) / rate;

        string details =
          $"{count:N0}/{nSamples:N0} samples ({progress:P1}) - {rate:F0} samples/sec, ETA {TimeSpan.FromSeconds(eta):mm\\:ss}";

        lock (lockObj)
        {
          Console.WriteLine($"  Smooth k-NN: {details}");
        }

        progressReporter?.Report(("Smooth k-NN Distances", progress, details));
      }
      else if (count % reportInterval == 0)
      {
        progressReporter?.Report(("Smooth k-NN Distances", (float)count / nSamples, null));
      }
    };
  }

  /// <summary>
  /// Computes smooth k-nearest neighbor distances using Newton's method (parallelized).
  /// </summary>
  /// <remarks>
  /// This computes a continuous version of the distance to the kth nearest neighbor.
  /// The result is calibrated so that the sum of probabilities equals log2(k).
  /// Uses Newton's method for 3-5x faster convergence than binary search.
  /// </remarks>
  public static (float[] Sigmas, float[] Rhos) SmoothKnnDist(
    float[][] knnDistances,
    int k,
    float localConnectivity = 1.0f,
    int verbosity = 1,
    IProgress<(string Stage, float Progress, string? Details)>? progressReporter = null
  )
  {
    int nSamples = knnDistances.Length;
    var sigmas = new float[nSamples];
    var rhos = new float[nSamples];
    float target = MathF.Log2(k);

    var stopwatch = Stopwatch.StartNew();
    int completed = 0;
    object lockObj = new object();

    // Setup progress reporting
    var reportProgress = CreateSmoothKnnProgressReporter(
      nSamples,
      stopwatch,
      verbosity,
      progressReporter,
      lockObj
    );

    // Parallelize the computation
    Parallel.For(
      0,
      nSamples,
      i =>
      {
        var (sigma, rho) = ComputeSmoothKnnForSample(knnDistances[i], localConnectivity, target);

        sigmas[i] = sigma;
        rhos[i] = rho;

        // Progress reporting
        int count = Interlocked.Increment(ref completed);
        reportProgress(count);
      }
    );

    stopwatch.Stop();

    if (verbosity >= 1)
    {
      Console.WriteLine(
        $"Smooth k-NN complete: {nSamples:N0} samples in {stopwatch.Elapsed:mm\\:ss\\.ff} ({nSamples / stopwatch.Elapsed.TotalSeconds:F0} samples/sec)"
      );
    }

    return (sigmas, rhos);
  }

  /// <summary>
  /// Computes membership strengths for the fuzzy simplicial set (parallelized).
  /// </summary>
  public static SparseMatrix ComputeMembershipStrengths(
    int[][] knnIndices,
    float[][] knnDistances,
    float[] sigmas,
    float[] rhos,
    float setOpMixRatio = 1.0f,
    int verbosity = 1,
    IProgress<(string Stage, float Progress, string? Details)>? progressReporter = null
  )
  {
    int nSamples = knnIndices.Length;
    int nNeighbors = knnIndices[0].Length;

    if (verbosity >= 1)
    {
      Console.WriteLine($"Computing membership strengths for {nSamples:N0} samples...");
    }

    // OPTIMIZATION: Pre-allocate arrays instead of using List<T> with repeated Add()
    // Worst case: nSamples * nNeighbors edges (minus self-loops)
    int maxEdges = nSamples * nNeighbors;
    object lockObj = new object();

    // OPTIMIZATION: Parallelize membership strength computation
    var localResults = new List<(int[] rows, int[] cols, float[] vals, int count)>();

    Parallel.For(
      0,
      nSamples,
      () =>
        (
          rows: new int[nNeighbors],
          cols: new int[nNeighbors],
          vals: new float[nNeighbors],
          count: 0
        ),
      (i, loopState, local) =>
      {
        int localCount = 0;

        for (int j = 0; j < nNeighbors; j++)
        {
          int neighbor = knnIndices[i][j];
          if (neighbor == i)
          {
            continue; // Skip self-loops
          }

          float dist = knnDistances[i][j];
          float d = Math.Max(0f, dist - rhos[i]);
          float val = MathF.Exp(-d / sigmas[i]);

          if (val > 0f)
          {
            local.rows[localCount] = i;
            local.cols[localCount] = neighbor;
            local.vals[localCount] = val;
            localCount++;
          }
        }

        return (local.rows, local.cols, local.vals, localCount);
      },
      local =>
      {
        lock (lockObj)
        {
          localResults.Add(local);
        }
      }
    );

    // Merge all thread-local results
    int totalEdges = localResults.Sum(r => r.count);
    var finalRows = new int[totalEdges];
    var finalCols = new int[totalEdges];
    var finalVals = new float[totalEdges];
    int offset = 0;

    foreach (var (localRows, localCols, localVals, count) in localResults)
    {
      Array.Copy(localRows, 0, finalRows, offset, count);
      Array.Copy(localCols, 0, finalCols, offset, count);
      Array.Copy(localVals, 0, finalVals, offset, count);
      offset += count;
    }

    if (verbosity >= 1)
    {
      Console.WriteLine($"Building sparse matrix from {totalEdges:N0} edges...");
    }

    // Create sparse matrix from triplets
    var tuplesList = finalRows
      .Zip(finalCols)
      .Zip(finalVals, (rc, v) => new Tuple<int, int, float>(rc.First, rc.Second, v));
    var matrix = SparseMatrix.OfIndexed(nSamples, nSamples, tuplesList);

    if (verbosity >= 1)
    {
      Console.WriteLine($"Symmetrizing graph via fuzzy set union...");
    }

    // Symmetrize using set operations
    var transpose = matrix.Transpose() as SparseMatrix ?? throw new InvalidOperationException();

    if (verbosity >= 2)
    {
      Console.WriteLine($"  Computing transpose complete");
    }

    var prodMatrix = matrix.PointwiseMultiply(transpose);

    if (verbosity >= 2)
    {
      Console.WriteLine($"  Computing intersection (pointwise multiply) complete");
    }

    var result = (matrix + transpose - prodMatrix)
      .Multiply(setOpMixRatio)
      .Add(prodMatrix.Multiply(1.0f - setOpMixRatio));

    if (verbosity >= 1)
    {
      Console.WriteLine($"Graph symmetrization complete");
    }

    return result as SparseMatrix ?? throw new InvalidOperationException();
  }

  /// <summary>
  /// Constructs the fuzzy simplicial set from k-nearest neighbor data.
  /// </summary>
  public static SparseMatrix FuzzySimplicialSetFromKnn(
    int[][] knnIndices,
    float[][] knnDistances,
    int nNeighbors,
    float localConnectivity = 1.0f,
    float setOpMixRatio = 1.0f,
    int verbosity = 1,
    IProgress<(string Stage, float Progress, string? Details)>? progressReporter = null
  )
  {
    // Compute smooth k-nearest neighbor distances (parallelized)
    var (sigmas, rhos) = SmoothKnnDist(
      knnDistances,
      nNeighbors,
      localConnectivity,
      verbosity,
      progressReporter
    );

    // Compute membership strengths and build sparse matrix (parallelized)
    var graph = ComputeMembershipStrengths(
      knnIndices,
      knnDistances,
      sigmas,
      rhos,
      setOpMixRatio,
      verbosity,
      progressReporter
    );

    return graph;
  }
}
