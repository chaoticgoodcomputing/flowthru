using MathNet.Numerics.LinearAlgebra.Single;
using MathNet.Numerics.LinearAlgebra.Storage;

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
/// <b>Implementation:</b> Uses direct CSR (Compressed Sparse Row) storage manipulation for
/// O(nnz) performance. Single-pass filter through non-zero entries only, avoiding O(n²) iteration.
/// </para>
/// <para>
/// <b>Time complexity:</b> O(nnz) where nnz is the number of non-zero entries in the graph
/// </para>
/// <para>
/// <b>Space complexity:</b> O(nnz) - creates new storage arrays during filtering
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
  /// Refines the graph by removing edges below an adaptive threshold using CSR direct access.
  /// </summary>
  /// <param name="graph">Fuzzy simplicial set to refine (modified in-place).</param>
  /// <param name="nEpochs">Number of optimization epochs planned.</param>
  /// <returns>Refinement result with statistics.</returns>
  public GraphRefinementResult RefineGraph(SparseMatrix graph, int nEpochs)
  {
    Console.WriteLine(
      $"[AdaptiveThresholding] RefineGraph called (rows={graph.RowCount}, nnz={graph.NonZerosCount}, epochs={nEpochs})"
    );

    ValidateInputs(graph, nEpochs);

    Console.WriteLine($"[AdaptiveThresholding] Validation passed, extracting CSR storage");

    // Extract CSR storage
    if (graph.Storage is not SparseCompressedRowMatrixStorage<float> storage)
    {
      throw new InvalidOperationException(
        "Graph must use CSR (SparseCompressedRowMatrixStorage) format. "
          + $"Found: {graph.Storage.GetType().Name}"
      );
    }

    Console.WriteLine($"[AdaptiveThresholding] Computing threshold...");

    // Compute threshold
    var maxWeight = ComputeMaxWeight(storage);
    var threshold = ComputeThreshold(maxWeight, nEpochs);

    Console.WriteLine(
      $"[AdaptiveThresholding] Threshold computed: {threshold:F6} (maxWeight={maxWeight:F4}, epochs={nEpochs})"
    );

    Console.WriteLine($"[AdaptiveThresholding] Filtering CSR storage...");

    // Filter edges in single pass through CSR arrays
    var (newStorage, edgesRemoved) = FilterCsrStorage(storage, threshold);

    Console.WriteLine(
      $"[AdaptiveThresholding] Filtered {edgesRemoved} edges, creating refined graph..."
    );

    // Replace the graph's storage with filtered version
    // Create new matrix from storage and copy back to maintain in-place semantics
    var refinedGraph = new SparseMatrix(newStorage);

    Console.WriteLine($"[AdaptiveThresholding] Copying refined graph back to original matrix...");

    // Copy filtered data back into original matrix
    graph.Clear();
    graph.SetSubMatrix(0, 0, refinedGraph);

    Console.WriteLine($"[AdaptiveThresholding] RefineGraph completed successfully");

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
  /// Computes the maximum edge weight by scanning CSR values array.
  /// </summary>
  /// <remarks>
  /// O(nnz) scan through values array - much faster than O(n²) matrix iteration.
  /// </remarks>
  private static float ComputeMaxWeight(SparseCompressedRowMatrixStorage<float> storage)
  {
    var values = storage.Values;
    float maxWeight = 0.0f;

    for (int i = 0; i < storage.ValueCount; i++)
    {
      if (values[i] > maxWeight)
      {
        maxWeight = values[i];
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
  /// Filters CSR storage arrays by threshold in a single pass.
  /// </summary>
  /// <remarks>
  /// <para>
  /// This is the core optimization: instead of iterating over all n² positions,
  /// we iterate only over the nnz non-zero entries and copy those above threshold.
  /// </para>
  /// <para>
  /// <b>Algorithm:</b>
  /// </para>
  /// <code>
  /// For each row:
  ///   Read entries from old storage [rowStart..rowEnd)
  ///   Copy entries >= threshold to new arrays
  ///   Update new RowPointers[row] with write position
  /// </code>
  /// </remarks>
  private static (
    SparseCompressedRowMatrixStorage<float> newStorage,
    int edgesRemoved
  ) FilterCsrStorage(SparseCompressedRowMatrixStorage<float> storage, float threshold)
  {
    int nRows = storage.RowCount;
    int nCols = storage.ColumnCount;

    // Pre-allocate with current capacity (will shrink)
    var newValues = new List<float>(storage.ValueCount);
    var newColumnIndices = new List<int>(storage.ValueCount);
    var newRowPointers = new int[nRows + 1];

    int edgesRemoved = 0;
    int writePos = 0;

    // Single pass through all rows
    for (int row = 0; row < nRows; row++)
    {
      newRowPointers[row] = writePos;

      int rowStart = storage.RowPointers[row];
      int rowEnd = storage.RowPointers[row + 1];

      // Process all entries in this row
      for (int idx = rowStart; idx < rowEnd; idx++)
      {
        float value = storage.Values[idx];

        if (value >= threshold)
        {
          // Keep this edge
          newValues.Add(value);
          newColumnIndices.Add(storage.ColumnIndices[idx]);
          writePos++;
        }
        else
        {
          // Remove this edge
          edgesRemoved++;
        }
      }
    }

    // Final row pointer marks end of data
    newRowPointers[nRows] = writePos;

    // Create new CSR storage with filtered data using indexed format
    // Build indexed enumerable from the filtered CSR arrays
    var indexedEntries = new List<(int row, int col, float value)>(writePos);
    for (int row = 0; row < nRows; row++)
    {
      int start = newRowPointers[row];
      int end = newRowPointers[row + 1];
      for (int idx = start; idx < end; idx++)
      {
        indexedEntries.Add((row, newColumnIndices[idx], newValues[idx]));
      }
    }

    var newStorage = SparseCompressedRowMatrixStorage<float>.OfIndexedEnumerable(
      nRows,
      nCols,
      indexedEntries
    );

    return (newStorage, edgesRemoved);
  }
}
