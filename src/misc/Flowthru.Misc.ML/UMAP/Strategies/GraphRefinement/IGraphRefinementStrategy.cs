using MathNet.Numerics.LinearAlgebra.Single;

namespace Flowthru.Misc.ML.UMAP.Strategies.GraphRefinement;

/// <summary>
/// Strategy interface for refining the fuzzy simplicial set graph before layout optimization.
/// This is the fourth phase of the UMAP algorithm.
/// </summary>
/// <remarks>
/// <para>
/// The graph refinement phase prepares the fuzzy simplicial set for layout optimization by:
/// </para>
/// <list type="bullet">
///   <item><description>Pruning weak edges that would have minimal impact on optimization</description></item>
///   <item><description>Reducing memory footprint and computational cost</description></item>
///   <item><description>Improving numerical stability by removing near-zero weights</description></item>
/// </list>
/// <para>
/// <b>Standard approach (adaptive thresholding):</b>
/// </para>
/// <para>
/// Edges with weight below <c>max_weight / n_epochs</c> are removed, as they would be
/// sampled less than once during optimization. This balances graph sparsity with fidelity.
/// </para>
/// <para>
/// Python UMAP reference: Lines 1063-1076 in <c>simplicial_set_embedding()</c> function
/// </para>
/// </remarks>
public interface IGraphRefinementStrategy
{
  /// <summary>
  /// Refines the fuzzy simplicial set by pruning weak edges and normalizing edge weights.
  /// </summary>
  /// <param name="graph">
  /// Input fuzzy simplicial set as a sparse symmetric matrix.
  /// Shape: (n_samples, n_samples)
  /// This matrix may be modified in-place for efficiency.
  /// </param>
  /// <param name="nEpochs">
  /// Number of optimization epochs planned for layout optimization.
  /// Used to determine the minimum edge weight threshold - edges sampled less than
  /// once during optimization can be safely removed.
  /// Must be positive.
  /// </param>
  /// <returns>
  /// A refined sparse graph with weak edges removed and remaining edges normalized.
  /// May return the same instance as input if modified in-place.
  /// </returns>
  /// <remarks>
  /// <para>
  /// <b>Implementation requirements:</b>
  /// </para>
  /// <list type="number">
  ///   <item><description>Determine edge weight threshold based on n_epochs</description></item>
  ///   <item><description>Remove edges below threshold</description></item>
  ///   <item><description>Eliminate zero entries from sparse matrix</description></item>
  ///   <item><description>Preserve matrix symmetry</description></item>
  ///   <item><description>Thread-safe for concurrent refinement operations</description></item>
  /// </list>
  /// <para>
  /// <b>Performance considerations:</b>
  /// </para>
  /// <list type="bullet">
  ///   <item><description>In-place modification is preferred to reduce memory allocation</description></item>
  ///   <item><description>Sparse matrix operations should preserve CSR/CSC format efficiency</description></item>
  /// </list>
  /// </remarks>
  GraphRefinementResult RefineGraph(SparseMatrix graph, int nEpochs);
}

/// <summary>
/// Result of graph refinement operation.
/// </summary>
/// <param name="RefinedGraph">
/// The refined sparse graph with weak edges removed.
/// Shape: (n_samples, n_samples)
/// </param>
/// <param name="EdgesRemoved">
/// Number of edges removed during refinement.
/// Useful for diagnostics and logging.
/// </param>
/// <param name="MinEdgeWeight">
/// The minimum edge weight threshold that was applied.
/// Edges below this value were removed.
/// </param>
public sealed record GraphRefinementResult(
  SparseMatrix RefinedGraph,
  int EdgesRemoved,
  float MinEdgeWeight
);
