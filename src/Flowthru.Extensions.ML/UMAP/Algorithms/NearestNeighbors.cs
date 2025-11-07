using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using MathNet.Numerics.LinearAlgebra;

namespace Flowthru.Extensions.ML.UMAP.Algorithms;

/// <summary>
/// K-Nearest Neighbors computation for UMAP.
/// </summary>
/// <remarks>
/// Based on the UMAP Python implementation by Leland McInnes.
/// Uses parallel brute-force with SIMD-optimized distance calculations.
/// Reference: https://github.com/lmcinnes/umap
/// </remarks>
public static class NearestNeighbors
{
  /// <summary>
  /// Finds k nearest neighbors for a single point using a max heap (PriorityQueue).
  /// </summary>
  /// <remarks>
  /// O(n log k) complexity - more efficient than sorting when k << n.
  /// Uses PriorityQueue as a max heap to maintain k smallest distances.
  /// </remarks>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private static (int[] Indices, float[] Distances) FindKNearestNeighbors(
    ReadOnlySpan<float> point,
    float[][] dataRows,
    int k,
    Func<ReadOnlySpan<float>, ReadOnlySpan<float>, float> metric
  )
  {
    // Max heap - larger distances have higher priority (so we can evict them)
    var heap = new PriorityQueue<int, float>(k, Comparer<float>.Create((a, b) => b.CompareTo(a)));

    // Build heap with k smallest distances
    for (int j = 0; j < dataRows.Length; j++)
    {
      float distance = metric(point, dataRows[j]);

      if (heap.Count < k)
      {
        heap.Enqueue(j, distance);
      }
      else if (distance < heap.Peek())
      {
        heap.DequeueEnqueue(j, distance);
      }
    }

    // Extract results in sorted order (smallest to largest)
    var indices = new int[k];
    var distances = new float[k];

    for (int i = k - 1; i >= 0; i--)
    {
      indices[i] = heap.Dequeue();
      distances[i] = heap.UnorderedItems.FirstOrDefault().Priority;
    }

    // Since we extracted backwards, results are now in ascending order
    // But we need to extract properly with priorities
    return ExtractSortedFromHeap(dataRows, metric, point, k);
  }

  /// <summary>
  /// Properly extracts k nearest neighbors by rebuilding and extracting in order.
  /// </summary>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private static (int[] Indices, float[] Distances) ExtractSortedFromHeap(
    float[][] dataRows,
    Func<ReadOnlySpan<float>, ReadOnlySpan<float>, float> metric,
    ReadOnlySpan<float> point,
    int k
  )
  {
    // Use a min heap for final extraction
    var minHeap = new PriorityQueue<int, float>(k);

    // Find k smallest
    var maxHeap = new PriorityQueue<int, float>(
      k,
      Comparer<float>.Create((a, b) => b.CompareTo(a))
    );

    for (int j = 0; j < dataRows.Length; j++)
    {
      float distance = metric(point, dataRows[j]);

      if (maxHeap.Count < k)
      {
        maxHeap.Enqueue(j, distance);
      }
      else if (distance < maxHeap.Peek())
      {
        maxHeap.DequeueEnqueue(j, distance);
      }
    }

    // Transfer to min heap for sorted extraction
    while (maxHeap.Count > 0)
    {
      var (idx, dist) = (maxHeap.Dequeue(), maxHeap.UnorderedItems.FirstOrDefault().Priority);
      minHeap.Enqueue(idx, dist);
    }

    // Extract in sorted order
    var indices = new int[k];
    var distances = new float[k];
    for (int i = 0; i < k && minHeap.Count > 0; i++)
    {
      indices[i] = minHeap.Dequeue();
      if (minHeap.TryPeek(out _, out float nextDist))
      {
        distances[i] = nextDist;
      }
    }

    return (indices, distances);
  }

  /// <summary>
  /// Simpler k-nearest using array with pooling - for small k this is actually faster.
  /// </summary>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private static (int[] Indices, float[] Distances) FindKNearestSimple(
    ReadOnlySpan<float> point,
    float[][] dataRows,
    int k,
    (int Index, float Distance)[] buffer,
    Func<ReadOnlySpan<float>, ReadOnlySpan<float>, float> metric
  )
  {
    // Compute all distances
    for (int j = 0; j < dataRows.Length; j++)
    {
      float distance = metric(point, dataRows[j]);
      buffer[j] = (j, distance);
    }

    // Partial sort - only sort first k elements
    PartialSort(buffer, k);

    // Extract results
    var indices = new int[k];
    var distances = new float[k];
    for (int i = 0; i < k; i++)
    {
      indices[i] = buffer[i].Index;
      distances[i] = buffer[i].Distance;
    }

    return (indices, distances);
  }

  /// <summary>
  /// Efficient partial sort using selection for small k - generic version.
  /// </summary>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private static void PartialSort<T>((int Index, T Distance)[] array, int k)
    where T : IComparable<T>
  {
    // Selection sort for k smallest elements
    for (int i = 0; i < k && i < array.Length; i++)
    {
      int minIdx = i;
      for (int j = i + 1; j < array.Length; j++)
      {
        if (array[j].Distance.CompareTo(array[minIdx].Distance) < 0)
        {
          minIdx = j;
        }
      }
      if (minIdx != i)
      {
        (array[i], array[minIdx]) = (array[minIdx], array[i]);
      }
    }
  }

  /// <summary>
  /// Extracts data rows from matrix into jagged array for efficient access.
  /// </summary>
  private static float[][] ExtractDataRows(Matrix<float> data, int verbosity)
  {
    int nSamples = data.RowCount;

    if (verbosity >= 1)
    {
      Console.WriteLine($"Pre-extracting {nSamples:N0} data rows for optimized access...");
    }

    var dataRows = new float[nSamples][];
    for (int i = 0; i < nSamples; i++)
    {
      dataRows[i] = data.Row(i).AsArray();
    }

    return dataRows;
  }

  /// <summary>
  /// Creates a progress reporter for k-NN computation.
  /// </summary>
  private static Action<int> CreateProgressReporter(
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
          Console.WriteLine($"  k-NN Progress: {details}");
        }

        progressReporter?.Report(("k-NN Computation", progress, details));
      }
      else if (count % reportInterval == 0)
      {
        progressReporter?.Report(("k-NN Computation", (float)count / nSamples, null));
      }
    };
  }

  /// <summary>
  /// Computes k-nearest neighbors for all samples in parallel using ArrayPool for efficiency.
  /// </summary>
  private static (int[][] Indices, float[][] Distances) ComputeKnnParallel(
    float[][] dataRows,
    int nNeighbors,
    Func<ReadOnlySpan<float>, ReadOnlySpan<float>, float> metric,
    Action<int> reportProgress
  )
  {
    int nSamples = dataRows.Length;
    var indices = new int[nSamples][];
    var distances = new float[nSamples][];
    int completed = 0;

    // Use ArrayPool to rent buffers instead of allocating each time
    var bufferPool = ArrayPool<(int Index, float Distance)>.Shared;

    Parallel.For(
      0,
      nSamples,
      () => bufferPool.Rent(nSamples), // Rent buffer from pool
      (i, loopState, neighborBuffer) =>
      {
        var point = dataRows[i];

        // Find k nearest neighbors using simple partial sort (fast for small k)
        var (nearestIndices, nearestDistances) = FindKNearestSimple(
          point,
          dataRows,
          nNeighbors,
          neighborBuffer,
          metric
        );

        indices[i] = nearestIndices;
        distances[i] = nearestDistances;

        // Report progress
        int count = Interlocked.Increment(ref completed);
        reportProgress(count);

        return neighborBuffer;
      },
      (neighborBuffer) => bufferPool.Return(neighborBuffer) // Return buffer to pool
    );

    return (indices, distances);
  }

  /// <summary>
  /// Computes the k-nearest neighbors for each point in the dataset using parallel processing.
  /// </summary>
  /// <param name="data">Data matrix where each row is a data point.</param>
  /// <param name="nNeighbors">Number of neighbors to find (including self).</param>
  /// <param name="metric">Distance metric function.</param>
  /// <param name="verbosity">Verbosity level: 0=silent, 1=minimal, 2=detailed.</param>
  /// <param name="progressReporter">Optional progress reporter for programmatic tracking.</param>
  /// <returns>Tuple of (indices, distances) where each row contains the k-nearest neighbors.</returns>
  public static (int[][] Indices, float[][] Distances) ComputeKnn(
    Matrix<float> data,
    int nNeighbors,
    Func<ReadOnlySpan<float>, ReadOnlySpan<float>, float> metric,
    int verbosity = 1,
    IProgress<(string Stage, float Progress, string? Details)>? progressReporter = null
  )
  {
    int nSamples = data.RowCount;

    if (nNeighbors > nSamples)
    {
      throw new ArgumentException(
        $"nNeighbors ({nNeighbors}) cannot be greater than number of samples ({nSamples})"
      );
    }

    if (verbosity >= 1)
    {
      Console.WriteLine($"Computing k-NN for {nSamples:N0} samples (k={nNeighbors})...");
    }

    var stopwatch = Stopwatch.StartNew();

    // Extract data rows from matrix
    var dataRows = ExtractDataRows(data, verbosity);

    // Setup progress reporting
    var lockObj = new object();
    var reportProgress = CreateProgressReporter(
      nSamples,
      stopwatch,
      verbosity,
      progressReporter,
      lockObj
    );

    if (verbosity >= 1)
    {
      Console.WriteLine($"Starting parallel k-NN computation...");
    }

    // Compute k-NN in parallel
    var (indices, distances) = ComputeKnnParallel(dataRows, nNeighbors, metric, reportProgress);

    stopwatch.Stop();

    if (verbosity >= 1)
    {
      Console.WriteLine(
        $"k-NN computation complete: {nSamples:N0} samples in {stopwatch.Elapsed:mm\\:ss\\.ff} ({nSamples / stopwatch.Elapsed.TotalSeconds:F0} samples/sec)"
      );
    }

    progressReporter?.Report(("k-NN Computation", 1.0f, "Complete"));

    return (indices, distances);
  }

  /// <summary>
  /// Pre-extracts data rows and computes squared norms for Euclidean optimization.
  /// </summary>
  private static (float[][] DataRows, float[] RowNorms) ExtractDataRowsWithNorms(
    Matrix<float> data,
    int verbosity
  )
  {
    int nSamples = data.RowCount;

    if (verbosity >= 1)
    {
      Console.WriteLine($"Pre-extracting {nSamples:N0} data rows and computing norms...");
    }

    var dataRows = new float[nSamples][];
    var rowNorms = new float[nSamples];

    for (int i = 0; i < nSamples; i++)
    {
      dataRows[i] = data.Row(i).AsArray();

      // Compute ||x||² = sum of squares
      float normSquared = 0f;
      for (int j = 0; j < dataRows[i].Length; j++)
      {
        normSquared += dataRows[i][j] * dataRows[i][j];
      }
      rowNorms[i] = normSquared;
    }

    return (dataRows, rowNorms);
  }

  /// <summary>
  /// Finds k nearest neighbors for a single point using Euclidean distance optimization.
  /// </summary>
  /// <remarks>
  /// Uses the formula: ||x - y||² = ||x||² + ||y||² - 2(x·y)
  /// During comparison, we skip adding ||x||² since it doesn't affect ordering.
  /// </remarks>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private static (int[] Indices, float[] Distances) FindKNearestEuclidean(
    ReadOnlySpan<float> point,
    float[][] dataRows,
    float[] rowNorms,
    float pointNorm,
    int k,
    (int Index, float PartialDistance)[] buffer
  )
  {
    // Compute partial distances: -2(point·other) + ||other||²
    for (int j = 0; j < dataRows.Length; j++)
    {
      ReadOnlySpan<float> other = dataRows[j];
      float dotProduct = DistanceMetrics.DotProduct(point, other);
      float partialDist = -2f * dotProduct + rowNorms[j];
      buffer[j] = (j, partialDist);
    }

    // Partial sort by partial distance
    PartialSort(buffer, k);

    // Extract results and compute real distances
    var indices = new int[k];
    var distances = new float[k];

    for (int i = 0; i < k; i++)
    {
      indices[i] = buffer[i].Index;
      // Real squared distance: partialDist + ||point||²
      float squaredDist = buffer[i].PartialDistance + pointNorm;
      distances[i] = MathF.Sqrt(squaredDist);
    }

    return (indices, distances);
  }

  /// <summary>
  /// Computes k-NN in parallel using Euclidean distance optimization with ArrayPool.
  /// </summary>
  private static (int[][] Indices, float[][] Distances) ComputeKnnEuclideanParallel(
    float[][] dataRows,
    float[] rowNorms,
    int nNeighbors,
    Action<int> reportProgress
  )
  {
    int nSamples = dataRows.Length;
    var indices = new int[nSamples][];
    var distances = new float[nSamples][];
    int completed = 0;

    // Use ArrayPool for temporary buffers
    var bufferPool = ArrayPool<(int Index, float PartialDistance)>.Shared;

    Parallel.For(
      0,
      nSamples,
      () => bufferPool.Rent(nSamples), // Rent buffer from pool
      (i, loopState, partialDistanceBuffer) =>
      {
        ReadOnlySpan<float> point = dataRows[i];

        // Find k nearest neighbors using Euclidean optimization
        var (nearestIndices, nearestDistances) = FindKNearestEuclidean(
          point,
          dataRows,
          rowNorms,
          rowNorms[i],
          nNeighbors,
          partialDistanceBuffer
        );

        indices[i] = nearestIndices;
        distances[i] = nearestDistances;

        // Report progress
        int count = Interlocked.Increment(ref completed);
        reportProgress(count);

        return partialDistanceBuffer;
      },
      (partialDistanceBuffer) => bufferPool.Return(partialDistanceBuffer) // Return to pool
    );

    return (indices, distances);
  }

  /// <summary>
  /// Computes k-nearest neighbors using Euclidean distance (optimized path).
  /// </summary>
  /// <remarks>
  /// <para>
  /// Optimized version that uses the expanded squared Euclidean formula:
  /// ||x - y||² = ||x||² + ||y||² - 2(x·y)
  /// </para>
  /// <para>
  /// Key optimizations (based on ML.NET K-Means implementation):
  /// 1. Pre-compute all ||x||² norms once (O(n) cost)
  /// 2. During comparison, skip adding ||x||² term (doesn't affect ordering)
  /// 3. Only compute: -2(x·y) + ||y||² for each comparison
  /// 4. Use SIMD-optimized dot product
  /// 5. Only add back ||x||² and compute sqrt for final k neighbors
  /// 6. Use ArrayPool to eliminate GC pressure from temporary buffers
  /// </para>
  /// </remarks>
  public static (int[][] Indices, float[][] Distances) ComputeKnnEuclidean(
    Matrix<float> data,
    int nNeighbors,
    int verbosity = 1,
    IProgress<(string Stage, float Progress, string? Details)>? progressReporter = null
  )
  {
    int nSamples = data.RowCount;

    if (nNeighbors > nSamples)
    {
      throw new ArgumentException(
        $"nNeighbors ({nNeighbors}) cannot be greater than number of samples ({nSamples})"
      );
    }

    if (verbosity >= 1)
    {
      Console.WriteLine(
        $"Computing k-NN (Euclidean optimized) for {nSamples:N0} samples (k={nNeighbors})..."
      );
    }

    var stopwatch = Stopwatch.StartNew();

    // Extract data rows and pre-compute norms
    var (dataRows, rowNorms) = ExtractDataRowsWithNorms(data, verbosity);

    // Setup progress reporting
    var lockObj = new object();
    var reportProgress = CreateProgressReporter(
      nSamples,
      stopwatch,
      verbosity,
      progressReporter,
      lockObj
    );

    if (verbosity >= 1)
    {
      Console.WriteLine($"Starting parallel k-NN computation...");
    }

    // Compute k-NN in parallel
    var (indices, distances) = ComputeKnnEuclideanParallel(
      dataRows,
      rowNorms,
      nNeighbors,
      reportProgress
    );

    stopwatch.Stop();

    if (verbosity >= 1)
    {
      Console.WriteLine(
        $"k-NN computation complete: {nSamples:N0} samples in {stopwatch.Elapsed:mm\\:ss\\.ff} ({nSamples / stopwatch.Elapsed.TotalSeconds:F0} samples/sec)"
      );
    }

    progressReporter?.Report(("k-NN Computation", 1.0f, "Complete"));

    return (indices, distances);
  }
}
