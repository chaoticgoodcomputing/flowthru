using Flowthru.Extensions.ML.UMAP.Core.Markers;

namespace Flowthru.Extensions.ML.UMAP.Strategies.NeighborSearch.Implementations;

/// <summary>
/// NN-Descent approximate k-nearest neighbor search.
/// Achieves ~99% accuracy with O(n^1.14) time complexity for large datasets.
/// </summary>
/// <remarks>
/// <para>
/// NN-Descent is an iterative algorithm that efficiently constructs approximate k-nearest neighbor
/// graphs through a local join operation. It achieves sub-quadratic time complexity while maintaining
/// high accuracy (typically 99%+ recall).
/// </para>
/// <para>
/// <b>Algorithm overview:</b>
/// </para>
/// <list type="number">
///   <item><description>Initialize with random projection trees (RP-trees) for quality starting neighbors</description></item>
///   <item><description>Fill remaining slots with random neighbors</description></item>
///   <item><description>Iteratively refine via local join: compare candidate neighbor pairs</description></item>
///   <item><description>Converge when update rate falls below threshold</description></item>
/// </list>
/// <para>
/// <b>Performance characteristics:</b>
/// </para>
/// <list type="bullet">
///   <item><description><b>Time complexity</b>: O(n^1.14 × d) empirically, vs O(n² × d) for brute-force</description></item>
///   <item><description><b>Space complexity</b>: O(n × k + trees × n / leaf_size)</description></item>
///   <item><description><b>Accuracy</b>: ~99% (approximate, configurable via parameters)</description></item>
///   <item><description><b>Recommended for</b>: Large datasets (≥ 4096 samples)</description></item>
///   <item><description><b>Thread-safe</b>: No (constructs new index per call)</description></item>
/// </list>
/// <para>
/// Based on: Dong, Moses, and Li. "Efficient K-Nearest Neighbor Graph Construction for Generic
/// Similarity Measures" (WWW 2011). Implementation follows PyNNDescent reference.
/// </para>
/// <para>
/// Python reference: <c>nn_descent()</c> function in <c>pynndescent_.py</c> and supporting functions
/// in <c>utils.py</c> and <c>rp_trees.py</c> from the PyNNDescent library.
/// </para>
/// </remarks>
public sealed class NNDescentSearch : INeighborSearchStrategy
{
  /// <summary>
  /// Number of random projection trees to build for initialization.
  /// If 0 (default), auto-configures as: min(32, 5 + round(n^0.25)).
  /// More trees improve initialization quality but increase build time.
  /// </summary>
  /// <remarks>
  /// Python UMAP typically uses 5-32 trees depending on dataset size.
  /// Each tree costs O(n log n × d) to build.
  /// </remarks>
  public int NumTrees { get; init; } = 0;

  /// <summary>
  /// Maximum number of NN-descent iterations.
  /// If 0 (default), auto-configures as: max(5, round(log2(n))).
  /// More iterations improve accuracy but increase runtime.
  /// </summary>
  /// <remarks>
  /// Algorithm typically converges in 5-10 iterations via delta threshold.
  /// Early stopping prevents unnecessary work.
  /// </remarks>
  public int MaxIterations { get; init; } = 0;

  /// <summary>
  /// Maximum number of candidate neighbors to consider per point per iteration.
  /// If 0 (default), auto-configures as: min(60, k).
  /// Higher values improve accuracy but increase iteration cost.
  /// </summary>
  /// <remarks>
  /// Controls the breadth of the local join search. Typical values: 30-60.
  /// Each iteration costs O(n × max_candidates² × d).
  /// </remarks>
  public int MaxCandidates { get; init; } = 0;

  /// <summary>
  /// Leaf size for random projection trees.
  /// Smaller leaves increase tree depth and initialization quality.
  /// Typical range: 10-20.
  /// </summary>
  public int LeafSize { get; init; } = 10;

  /// <summary>
  /// Convergence threshold as fraction of total edges.
  /// Algorithm stops when updates per iteration drop below: delta × k × n.
  /// Typical value: 0.001 (0.1% of edges changing).
  /// </summary>
  public float DeltaThreshold { get; init; } = 0.001f;

  /// <summary>
  /// If true, uses block-based processing to reduce memory usage at cost of ~2x speed.
  /// If false, maintains in-memory set for faster duplicate checking.
  /// </summary>
  public bool LowMemory { get; init; } = true;

  /// <summary>
  /// If true, prints progress information to console during search.
  /// </summary>
  public bool Verbose { get; init; } = false;

  /// <inheritdoc />
  public NeighborSearchResult Search(float[][] data, int nNeighbors, IMetric metric, Random random)
  {
    int nSamples = data.Length;
    int nFeatures = data[0].Length;

    if (nNeighbors > nSamples)
    {
      throw new ArgumentException(
        $"Cannot find {nNeighbors} neighbors with only {nSamples} samples",
        nameof(nNeighbors)
      );
    }

    if (nNeighbors < 2)
    {
      throw new ArgumentException("Number of neighbors must be at least 2", nameof(nNeighbors));
    }

    // Auto-configure parameters based on dataset size
    int numTrees =
      NumTrees > 0 ? NumTrees : Math.Min(32, 5 + (int)Math.Round(Math.Pow(nSamples, 0.25)));
    int maxIters =
      MaxIterations > 0 ? MaxIterations : Math.Max(5, (int)Math.Round(Math.Log2(nSamples)));
    int maxCand = MaxCandidates > 0 ? MaxCandidates : Math.Min(60, nNeighbors);

    if (Verbose)
    {
      Console.WriteLine($"NN-Descent: n={nSamples}, k={nNeighbors}, d={nFeatures}");
      Console.WriteLine(
        $"  Trees: {numTrees}, Max iterations: {maxIters}, Max candidates: {maxCand}"
      );
      Console.WriteLine($"  Leaf size: {LeafSize}, Delta threshold: {DeltaThreshold}");
    }

    // Phase 1: Build RP-tree forest for initialization
    if (Verbose)
    {
      Console.WriteLine("Building RP-tree forest...");
    }
    var forest = BuildRpForest(data, numTrees, LeafSize, random);

    // Phase 2: Initialize heap with tree neighbors + random neighbors
    if (Verbose)
    {
      Console.WriteLine("Initializing k-NN heap...");
    }
    var heap = new KnnHeap(nSamples, nNeighbors);
    InitializeHeap(heap, nNeighbors, forest, data, metric, random);

    // Phase 3: NN-descent iterations with convergence detection
    if (Verbose)
    {
      Console.WriteLine("Running NN-descent iterations...");
    }
    NNDescentLoop(heap, data, nNeighbors, maxIters, maxCand, DeltaThreshold, metric, random);

    // Phase 4: Convert heap to sorted arrays
    if (Verbose)
    {
      Console.WriteLine("Finalizing results...");
    }
    var (indices, distances) = heap.DeheapSort();

    // Build search index for transform operations (future use)
    // For now, return null as search index is not yet implemented
    object? searchIndex = null;

    if (Verbose)
    {
      Console.WriteLine("NN-Descent complete.");
    }

    return new NeighborSearchResult(indices, distances, searchIndex);
  }

  /// <summary>
  /// Builds a forest of random projection trees for initialization.
  /// </summary>
  private RpTree[] BuildRpForest(float[][] data, int numTrees, int leafSize, Random random)
  {
    return RpTreeBuilder.BuildForest(data, numTrees, leafSize, random);
  }

  /// <summary>
  /// Initializes the k-NN heap with neighbors from RP-trees and random points.
  /// </summary>
  private void InitializeHeap(
    KnnHeap heap,
    int nNeighbors,
    RpTree[] forest,
    float[][] data,
    IMetric metric,
    Random random
  )
  {
    // Step 1: Initialize from RP-tree leaves
    InitializeFromRpTrees(heap, forest, data, metric);

    // Step 2: Fill remaining slots with random neighbors
    InitializeRandom(heap, nNeighbors, data, metric, random);
  }

  /// <summary>
  /// Initializes heap with neighbors from RP-tree leaves.
  /// Points that appear together in tree leaves are likely neighbors.
  /// </summary>
  /// <remarks>
  /// Parallelized across leaves for improved performance. Each leaf is processed
  /// independently, with thread-safe heap updates.
  /// </remarks>
  private void InitializeFromRpTrees(KnnHeap heap, RpTree[] forest, float[][] data, IMetric metric)
  {
    // Extract all leaves from all trees
    var allLeaves = new List<int[]>();
    foreach (var tree in forest)
    {
      allLeaves.AddRange(tree.GetLeafArray());
    }

    // Process leaves in parallel - each leaf is independent
    Parallel.ForEach(
      allLeaves,
      leaf =>
      {
        // For each leaf, compare all pairs of points
        for (int i = 0; i < leaf.Length; i++)
        {
          int p = leaf[i];
          if (p < 0)
          {
            continue;
          }

          for (int j = i + 1; j < leaf.Length; j++)
          {
            int q = leaf[j];
            if (q < 0)
            {
              continue;
            }

            // Compute distance using spans (zero allocation)
            float d = metric.Distance(data[p].AsSpan(), data[q].AsSpan());

            // Try to add to both heaps (thread-safe via per-sample locks)
            heap.TryPush(p, q, d, flag: 1);
            heap.TryPush(q, p, d, flag: 1);
          }
        }
      }
    );
  }

  /// <summary>
  /// Fills remaining empty slots in heap with random neighbors.
  /// </summary>
  private void InitializeRandom(
    KnnHeap heap,
    int nNeighbors,
    float[][] data,
    IMetric metric,
    Random random
  )
  {
    int nSamples = data.Length;

    for (int i = 0; i < nSamples; i++)
    {
      // Count how many valid neighbors we have
      int validCount = 0;
      for (int k = 0; k < nNeighbors; k++)
      {
        if (heap.Indices[i][k] >= 0)
        {
          validCount++;
        }
      }

      // Fill remaining slots with random points
      int needed = nNeighbors - validCount;
      int attempts = 0;
      int maxAttempts = needed * 10; // Avoid infinite loops

      while (needed > 0 && attempts < maxAttempts)
      {
        int candidate = random.Next(nSamples);
        attempts++;

        // Skip self
        if (candidate == i)
        {
          continue;
        }

        // Compute distance using spans (zero allocation)
        float d = metric.Distance(data[i].AsSpan(), data[candidate].AsSpan());

        // Try to add (TryPush will reject duplicates)
        if (heap.TryPush(i, candidate, d, flag: 1))
        {
          needed--;
        }
      }
    }
  }

  /// <summary>
  /// Runs the main NN-descent loop with candidate generation and local join.
  /// </summary>
  private void NNDescentLoop(
    KnnHeap heap,
    float[][] data,
    int nNeighbors,
    int maxIterations,
    int maxCandidates,
    float deltaThreshold,
    IMetric metric,
    Random random
  )
  {
    int nSamples = data.Length;
    int convergenceThreshold = (int)(deltaThreshold * nNeighbors * nSamples);

    for (int iter = 0; iter < maxIterations; iter++)
    {
      if (Verbose)
      {
        Console.WriteLine($"  Iteration {iter + 1}/{maxIterations}");
      }

      // Build candidate lists from current heap
      var (newCandidates, oldCandidates) = BuildCandidates(heap, maxCandidates, random);

      // Local join: compare candidate pairs
      int updates = LocalJoin(heap, newCandidates, oldCandidates, data, metric);

      if (Verbose)
      {
        Console.WriteLine($"    Updates: {updates} (threshold: {convergenceThreshold})");
      }

      // Check convergence
      if (updates <= convergenceThreshold)
      {
        if (Verbose)
        {
          Console.WriteLine($"  Converged after {iter + 1} iterations");
        }
        break;
      }

      // Clear new flags for next iteration
      heap.ClearFlags();
    }
  }

  /// <summary>
  /// Builds candidate neighbor lists for the local join operation.
  /// Separates neighbors into "new" (recently added) and "old" (previously explored).
  /// </summary>
  private (int[][], int[][]) BuildCandidates(KnnHeap heap, int maxCandidates, Random random)
  {
    int nSamples = heap.Indices.Length;
    int k = heap.Indices[0].Length;

    // Pre-allocate with capacity to reduce reallocations
    var newCandidates = new List<int>[nSamples];
    var oldCandidates = new List<int>[nSamples];

    for (int i = 0; i < nSamples; i++)
    {
      newCandidates[i] = new List<int>(maxCandidates);
      oldCandidates[i] = new List<int>(maxCandidates);
    }

    // For each point and its neighbors, add to candidate lists
    // Key insight: add both i->j and j->i (reverse neighbors)
    for (int i = 0; i < nSamples; i++)
    {
      for (int kIdx = 0; kIdx < k; kIdx++)
      {
        int j = heap.Indices[i][kIdx];
        if (j < 0)
        {
          break;
        }

        byte flag = heap.Flags[i][kIdx];

        // Use random priority for sampling if needed
        if (flag == 1)
        {
          // New neighbor
          TryAddCandidate(newCandidates[i], j, maxCandidates);
          TryAddCandidate(newCandidates[j], i, maxCandidates);
        }
        else
        {
          // Old neighbor
          TryAddCandidate(oldCandidates[i], j, maxCandidates);
          TryAddCandidate(oldCandidates[j], i, maxCandidates);
        }
      }
    }

    // Convert to arrays
    var newArray = new int[nSamples][];
    var oldArray = new int[nSamples][];

    for (int i = 0; i < nSamples; i++)
    {
      newArray[i] = newCandidates[i].ToArray();
      oldArray[i] = oldCandidates[i].ToArray();
    }

    return (newArray, oldArray);
  }

  /// <summary>
  /// Tries to add a candidate to the list, respecting max candidates limit.
  /// </summary>
  private void TryAddCandidate(List<int> candidates, int neighbor, int maxCandidates)
  {
    if (candidates.Count < maxCandidates && !candidates.Contains(neighbor))
    {
      candidates.Add(neighbor);
    }
  }

  /// <summary>
  /// Performs local join: compares all candidate pairs and updates heaps with improvements.
  /// Parallelized across samples for significant speedup on multi-core systems.
  /// </summary>
  private int LocalJoin(
    KnnHeap heap,
    int[][] newCandidates,
    int[][] oldCandidates,
    float[][] data,
    IMetric metric
  )
  {
    int nSamples = data.Length;
    int totalUpdates = 0;

    // Use Parallel.For with thread-safe accumulation of updates
    var localUpdates = new int[Environment.ProcessorCount];

    Parallel.For(
      0,
      nSamples,
      () => 0, // Thread-local accumulator
      (i, loopState, localUpdate) =>
      {
        int[] newCand = newCandidates[i];
        int[] oldCand = oldCandidates[i];

        // Compare all (new, new) pairs
        for (int j = 0; j < newCand.Length; j++)
        {
          int p = newCand[j];
          if (p < 0)
          {
            break;
          }

          for (int k = j + 1; k < newCand.Length; k++)
          {
            int q = newCand[k];
            if (q < 0)
            {
              break;
            }

            localUpdate += TryUpdate(heap, data, metric, p, q);
          }
        }

        // Compare all (new, old) pairs
        for (int j = 0; j < newCand.Length; j++)
        {
          int p = newCand[j];
          if (p < 0)
          {
            break;
          }

          for (int k = 0; k < oldCand.Length; k++)
          {
            int q = oldCand[k];
            if (q < 0)
            {
              break;
            }

            localUpdate += TryUpdate(heap, data, metric, p, q);
          }
        }

        return localUpdate;
      },
      localUpdate => Interlocked.Add(ref totalUpdates, localUpdate)
    );

    return totalUpdates;
  }

  /// <summary>
  /// Attempts to update heaps for points p and q if their distance improves current k-NN.
  /// </summary>
  private int TryUpdate(KnnHeap heap, float[][] data, IMetric metric, int p, int q)
  {
    // Early exit: check if distance might improve either heap
    float thresholdP = heap.Distances[p][0]; // max distance in p's heap
    float thresholdQ = heap.Distances[q][0]; // max distance in q's heap

    // Compute distance using spans (zero allocation)
    float d = metric.Distance(data[p].AsSpan(), data[q].AsSpan());

    int updates = 0;

    // Try to improve p's neighbors
    if (d < thresholdP)
    {
      if (heap.TryPush(p, q, d, flag: 1))
      {
        updates++;
      }
    }

    // Try to improve q's neighbors
    if (d < thresholdQ)
    {
      if (heap.TryPush(q, p, d, flag: 1))
      {
        updates++;
      }
    }

    return updates;
  }
}
