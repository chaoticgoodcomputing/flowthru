using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Flowthru.Extensions.ML.UMAP.Algorithms;

/// <summary>
/// Approximate k-nearest neighbors using Random Projection Trees.
/// </summary>
/// <remarks>
/// <para>
/// Uses a forest of random projection trees to efficiently find approximate nearest neighbors
/// in high-dimensional space. This provides significant speedup over brute-force search for
/// large datasets (>10k samples) at the cost of slight accuracy loss.
/// </para>
/// <para>
/// The algorithm:
/// 1. Builds a forest of binary trees by recursively splitting data with random hyperplanes
/// 2. Queries all trees to find candidate neighbors
/// 3. Refines candidates using exact distance calculations
/// </para>
/// <para>
/// Time complexity: O(n log n) build, O(log n) query per tree
/// Space complexity: O(n * num_trees)
/// </para>
/// <para>
/// Based on concepts from:
/// - Dasgupta & Freund, "Random projection trees and low dimensional manifolds" (2008)
/// - Bernhardsson, "Annoy: Approximate Nearest Neighbors in C++/Python" (2013)
/// </para>
/// </remarks>
public static class ApproximateNearestNeighbors
{
  /// <summary>
  /// Random projection tree node for space partitioning.
  /// </summary>
  private class RpTreeNode
  {
    public float[]? Hyperplane { get; set; } // Random hyperplane for splitting
    public float Offset { get; set; } // Offset for hyperplane decision
    public int[]? Indices { get; set; } // Leaf node: indices of data points
    public RpTreeNode? Left { get; set; } // Left child (points < offset)
    public RpTreeNode? Right { get; set; } // Right child (points >= offset)
    public bool IsLeaf => Indices != null;
  }

  /// <summary>
  /// Computes approximate k-nearest neighbors using Random Projection Trees.
  /// </summary>
  /// <param name="data">Data matrix where each row is a data point.</param>
  /// <param name="nNeighbors">Number of neighbors to find (including self).</param>
  /// <param name="nTrees">Number of trees in the forest (more trees = better accuracy but slower).</param>
  /// <param name="leafSize">Maximum number of points in a leaf node.</param>
  /// <param name="searchK">Number of nodes to search in each tree (higher = more accurate but slower).</param>
  /// <param name="verbosity">Verbosity level: 0=silent, 1=minimal, 2=detailed.</param>
  /// <param name="progressReporter">Optional progress reporter for programmatic tracking.</param>
  /// <param name="random">Random number generator for reproducibility.</param>
  /// <returns>Tuple of (indices, distances) where each row contains the k-nearest neighbors.</returns>
  public static (int[][] Indices, float[][] Distances) ComputeApproximateKnn(
    float[][] dataRows,
    int nNeighbors,
    int nTrees = 10,
    int leafSize = 10,
    int? searchK = null,
    int verbosity = 1,
    IProgress<(string Stage, float Progress, string? Details)>? progressReporter = null,
    Random? random = null
  )
  {
    random ??= new Random();
    int nSamples = dataRows.Length;
    searchK ??= nNeighbors * nTrees; // Default: search k*trees nodes

    if (nNeighbors > nSamples)
    {
      throw new ArgumentException(
        $"nNeighbors ({nNeighbors}) cannot be greater than number of samples ({nSamples})"
      );
    }

    if (verbosity >= 1)
    {
      Console.WriteLine(
        $"Computing approximate k-NN: {nSamples:N0} samples, {nTrees} trees, leaf_size={leafSize}"
      );
    }

    var stopwatch = Stopwatch.StartNew();

    // Build forest of random projection trees
    if (verbosity >= 1)
    {
      Console.WriteLine($"Building {nTrees} random projection trees...");
    }

    var forest = BuildForest(dataRows, nTrees, leafSize, random, verbosity, progressReporter);

    if (verbosity >= 1)
    {
      Console.WriteLine($"Forest built in {stopwatch.Elapsed:mm\\:ss\\.ff}");
      Console.WriteLine($"Querying trees for approximate neighbors...");
    }

    stopwatch.Restart();

    // Query forest for each point
    var indices = new int[nSamples][];
    var distances = new float[nSamples][];

    var bufferPool = ArrayPool<int>.Shared;

    int completed = 0;
    int reportInterval = Math.Max(1, nSamples / 20);

    Parallel.For(
      0,
      nSamples,
      () => bufferPool.Rent(nSamples), // Thread-local candidate buffer
      (i, loopState, candidateBuffer) =>
      {
        var point = dataRows[i];

        // Query all trees to get candidate neighbors
        var candidates = QueryForest(forest, point, searchK.Value, candidateBuffer);

        // Refine candidates with exact distances and find k nearest
        var (nearestIndices, nearestDistances) = RefineAndSelectKNearest(
          point,
          dataRows,
          candidates,
          nNeighbors
        );

        indices[i] = nearestIndices;
        distances[i] = nearestDistances;

        // Progress reporting
        int count = Interlocked.Increment(ref completed);
        if (verbosity >= 2 && count % reportInterval == 0)
        {
          float progress = (float)count / nSamples;
          double elapsed = stopwatch.Elapsed.TotalSeconds;
          double rate = count / elapsed;
          double eta = (nSamples - count) / rate;

          string details =
            $"{count:N0}/{nSamples:N0} samples ({progress:P1}) - {rate:F0} samples/sec, ETA {TimeSpan.FromSeconds(eta):mm\\:ss}";

          Console.WriteLine($"  Query Progress: {details}");
          progressReporter?.Report(("Approximate k-NN Query", progress, details));
        }

        return candidateBuffer;
      },
      (candidateBuffer) => bufferPool.Return(candidateBuffer)
    );

    stopwatch.Stop();

    if (verbosity >= 1)
    {
      Console.WriteLine(
        $"Approximate k-NN complete: {nSamples:N0} samples in {stopwatch.Elapsed:mm\\:ss\\.ff} ({nSamples / stopwatch.Elapsed.TotalSeconds:F0} samples/sec)"
      );
    }

    progressReporter?.Report(("Approximate k-NN", 1.0f, "Complete"));

    return (indices, distances);
  }

  /// <summary>
  /// Builds a forest of random projection trees.
  /// </summary>
  private static RpTreeNode[] BuildForest(
    float[][] data,
    int nTrees,
    int leafSize,
    Random random,
    int verbosity,
    IProgress<(string Stage, float Progress, string? Details)>? progressReporter
  )
  {
    var forest = new RpTreeNode[nTrees];
    int completed = 0;

    // Build trees in parallel
    Parallel.For(
      0,
      nTrees,
      new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
      () => new Random(random.Next()),
      (treeIdx, state, threadRandom) =>
      {
        // Initialize with all indices
        var allIndices = Enumerable.Range(0, data.Length).ToArray();

        // Build tree
        forest[treeIdx] = BuildTree(data, allIndices, leafSize, threadRandom);

        int count = Interlocked.Increment(ref completed);
        if (verbosity >= 2)
        {
          Console.WriteLine($"  Tree {count}/{nTrees} built");
        }

        progressReporter?.Report(("Building RP Trees", (float)count / nTrees, null));

        return threadRandom;
      },
      _ => { }
    );

    return forest;
  }

  /// <summary>
  /// Recursively builds a random projection tree.
  /// </summary>
  private static RpTreeNode BuildTree(float[][] data, int[] indices, int leafSize, Random random)
  {
    // Leaf node condition
    if (indices.Length <= leafSize)
    {
      return new RpTreeNode { Indices = indices };
    }

    int nDim = data[0].Length;

    // Generate random hyperplane (normalized)
    var hyperplane = new float[nDim];
    float normSquared = 0f;

    for (int d = 0; d < nDim; d++)
    {
      // Sample from standard normal distribution (Box-Muller transform)
      float u1 = (float)random.NextDouble();
      float u2 = (float)random.NextDouble();
      float randNormal = MathF.Sqrt(-2f * MathF.Log(u1)) * MathF.Cos(2f * MathF.PI * u2);

      hyperplane[d] = randNormal;
      normSquared += randNormal * randNormal;
    }

    // Normalize hyperplane
    float norm = MathF.Sqrt(normSquared);
    if (norm > 0)
    {
      for (int d = 0; d < nDim; d++)
      {
        hyperplane[d] /= norm;
      }
    }

    // Project all points onto hyperplane and find median
    var projections = new (int index, float projection)[indices.Length];
    for (int i = 0; i < indices.Length; i++)
    {
      int dataIdx = indices[i];
      float projection = DotProduct(data[dataIdx], hyperplane);
      projections[i] = (dataIdx, projection);
    }

    // Sort by projection and split at median
    Array.Sort(projections, (a, b) => a.projection.CompareTo(b.projection));
    int medianIdx = projections.Length / 2;
    float offset = projections[medianIdx].projection;

    // Split indices
    var leftIndices = new int[medianIdx];
    var rightIndices = new int[projections.Length - medianIdx];

    for (int i = 0; i < medianIdx; i++)
    {
      leftIndices[i] = projections[i].index;
    }

    for (int i = medianIdx; i < projections.Length; i++)
    {
      rightIndices[i - medianIdx] = projections[i].index;
    }

    // Recursively build subtrees
    return new RpTreeNode
    {
      Hyperplane = hyperplane,
      Offset = offset,
      Left = BuildTree(data, leftIndices, leafSize, random),
      Right = BuildTree(data, rightIndices, leafSize, random),
    };
  }

  /// <summary>
  /// Queries all trees in the forest and collects candidate neighbors.
  /// </summary>
  private static HashSet<int> QueryForest(
    RpTreeNode[] forest,
    float[] point,
    int searchK,
    int[] candidateBuffer
  )
  {
    var candidates = new HashSet<int>();

    foreach (var tree in forest)
    {
      // Search tree up to searchK/nTrees nodes per tree
      int nodesPerTree = Math.Max(1, searchK / forest.Length);
      QueryTree(tree, point, nodesPerTree, candidates);

      if (candidates.Count >= searchK)
      {
        break;
      }
    }

    return candidates;
  }

  /// <summary>
  /// Queries a single tree to find candidate neighbors.
  /// </summary>
  private static void QueryTree(
    RpTreeNode node,
    float[] point,
    int maxNodes,
    HashSet<int> candidates
  )
  {
    if (node.IsLeaf)
    {
      // Add all points in leaf
      foreach (var idx in node.Indices!)
      {
        candidates.Add(idx);
      }
      return;
    }

    // Compute projection
    float projection = DotProduct(point, node.Hyperplane!);

    // Traverse to appropriate child first
    if (projection < node.Offset)
    {
      QueryTree(node.Left!, point, maxNodes, candidates);
      if (candidates.Count < maxNodes)
      {
        QueryTree(node.Right!, point, maxNodes, candidates);
      }
    }
    else
    {
      QueryTree(node.Right!, point, maxNodes, candidates);
      if (candidates.Count < maxNodes)
      {
        QueryTree(node.Left!, point, maxNodes, candidates);
      }
    }
  }

  /// <summary>
  /// Refines candidate set with exact distances and selects k nearest.
  /// </summary>
  private static (int[] Indices, float[] Distances) RefineAndSelectKNearest(
    float[] point,
    float[][] data,
    HashSet<int> candidates,
    int k
  )
  {
    // Compute exact distances for all candidates
    var candidateDistances = new (int index, float distance)[candidates.Count];
    int i = 0;

    foreach (var candidateIdx in candidates)
    {
      float distance = EuclideanDistance(point, data[candidateIdx]);
      candidateDistances[i++] = (candidateIdx, distance);
    }

    // Sort and take k smallest
    Array.Sort(candidateDistances, (a, b) => a.distance.CompareTo(b.distance));

    int actualK = Math.Min(k, candidateDistances.Length);
    var indices = new int[actualK];
    var distances = new float[actualK];

    for (i = 0; i < actualK; i++)
    {
      indices[i] = candidateDistances[i].index;
      distances[i] = candidateDistances[i].distance;
    }

    return (indices, distances);
  }

  /// <summary>
  /// Computes dot product between two vectors.
  /// </summary>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private static float DotProduct(float[] a, float[] b)
  {
    float sum = 0f;
    for (int i = 0; i < a.Length; i++)
    {
      sum += a[i] * b[i];
    }
    return sum;
  }

  /// <summary>
  /// Computes Euclidean distance between two vectors.
  /// </summary>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private static float EuclideanDistance(float[] a, float[] b)
  {
    float sum = 0f;
    for (int i = 0; i < a.Length; i++)
    {
      float diff = a[i] - b[i];
      sum += diff * diff;
    }
    return MathF.Sqrt(sum);
  }
}
