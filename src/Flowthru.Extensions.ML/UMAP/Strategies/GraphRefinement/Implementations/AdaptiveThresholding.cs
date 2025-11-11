using MathNet.Numerics.LinearAlgebra.Single;

namespace Flowthru.Extensions.ML.UMAP.Strategies.GraphRefinement.Implementations;

/// <summary>
/// Standard UMAP graph refinement using adaptive threshold based on optimization epochs.
/// </summary>
/// <remarks>
/// <para>
/// This implementation follows the standard UMAP algorithm's approach to graph refinement:
/// edges with weight below <c>max_weight / n_epochs</c> are removed, as they would be
/// sampled less than once during the optimization process.
/// </para>
/// <para>
/// <b>Rationale:</b> During stochastic gradient descent, edges are sampled proportionally
/// to their weights. An edge with weight <c>w</c> in a graph with maximum weight <c>w_max</c>
/// will be sampled approximately <c>(w / w_max) × n_epochs</c> times. Edges sampled less
/// than once have negligible impact on the final embedding.
/// </para>
/// <para>
/// <b>Time complexity:</b> O(nnz) where nnz is the number of non-zero entries in the graph
/// </para>
/// <para>
/// <b>Space complexity:</b> O(1) - operates in-place on the input matrix
/// </para>
/// <para>
/// Python UMAP reference: Lines 1063-1076 in <c>simplicial_set_embedding()</c>
/// </para>
/// </remarks>
public sealed class AdaptiveThresholding : IGraphRefinementStrategy
{
  private const int DefaultEpochsForThreshold = 200;
  private const int MinEpochsForDynamicThreshold = 10;

  /// <summary>
  /// Refines the graph by removing edges below an adaptive threshold.
  /// </summary>
  /// <param name="graph">Fuzzy simplicial set to refine (modified in-place).</param>
  /// <param name="nEpochs">Number of optimization epochs planned.</param>
  /// <returns>Refinement result with statistics.</returns>
  public GraphRefinementResult RefineGraph(SparseMatrix graph, int nEpochs)
  {
    ValidateInputs(graph, nEpochs);

    var maxWeight = ComputeMaxWeight(graph);
    var threshold = ComputeThreshold(maxWeight, nEpochs);
    var edgesRemoved = ApplyThreshold(graph, threshold);

    return new GraphRefinementResult(
      RefinedGraph: graph,
      EdgesRemoved: edgesRemoved,
      MinEdgeWeight: threshold
    );
  }

  /// <summary>
  /// Validates that inputs are in acceptable ranges.
  /// </summary>
  private static void ValidateInputs(SparseMatrix graph, int nEpochs)
  {
    if (graph.RowCount != graph.ColumnCount)
    {
      throw new ArgumentException(
        $"Graph must be square, got {graph.RowCount}×{graph.ColumnCount}",
        nameof(graph)
      );
    }

    if (nEpochs <= 0)
    {
      throw new ArgumentException(
        $"Number of epochs must be positive, got {nEpochs}",
        nameof(nEpochs)
      );
    }
  }

  /// <summary>
  /// Computes the maximum edge weight in the graph.
  /// </summary>
  private static float ComputeMaxWeight(SparseMatrix graph)
  {
    var maxWeight = 0.0f;

    foreach (var (_, _, value) in graph.EnumerateIndexed())
    {
      if (value > maxWeight)
      {
        maxWeight = value;
      }
    }

    return maxWeight;
  }

  /// <summary>
  /// Computes the minimum edge weight threshold based on epochs and max weight.
  /// </summary>
  /// <remarks>
  /// Python UMAP uses different thresholds based on whether n_epochs is greater than 10.
  /// This prevents over-aggressive pruning for very short optimization runs.
  /// </remarks>
  private static float ComputeThreshold(float maxWeight, int nEpochs)
  {
    var divisor =
      nEpochs > MinEpochsForDynamicThreshold ? (float)nEpochs : (float)DefaultEpochsForThreshold;

    return maxWeight / divisor;
  }

  /// <summary>
  /// Applies the threshold to the graph, zeroing out edges below it.
  /// Returns the number of edges removed.
  /// </summary>
  private static int ApplyThreshold(SparseMatrix graph, float threshold)
  {
    var edgesRemoved = 0;

    // Zero out entries below threshold
    for (var i = 0; i < graph.RowCount; i++)
    {
      for (var j = 0; j < graph.ColumnCount; j++)
      {
        var value = graph[i, j];
        if (value > 0 && value < threshold)
        {
          graph[i, j] = 0;
          edgesRemoved++;
        }
      }
    }

    // Remove zeros from sparse storage to reclaim memory
    // This is critical for maintaining performance in subsequent operations
    if (edgesRemoved > 0)
    {
      EliminateZeros(graph);
    }

    return edgesRemoved;
  }

  /// <summary>
  /// Removes zero entries from the sparse matrix storage.
  /// </summary>
  /// <remarks>
  /// MathNet's SparseMatrix doesn't have a built-in EliminateZeros method,
  /// so we rebuild the matrix with only non-zero entries.
  /// </remarks>
  private static void EliminateZeros(SparseMatrix graph)
  {
    var builder = SparseMatrix.Build;
    var nonZeroEntries = new List<(int row, int col, float value)>();

    foreach (var (i, j, value) in graph.EnumerateIndexed())
    {
      if (value != 0)
      {
        nonZeroEntries.Add((i, j, value));
      }
    }

    // Clear and rebuild
    graph.Clear();
    foreach (var (i, j, value) in nonZeroEntries)
    {
      graph[i, j] = value;
    }
  }
}
