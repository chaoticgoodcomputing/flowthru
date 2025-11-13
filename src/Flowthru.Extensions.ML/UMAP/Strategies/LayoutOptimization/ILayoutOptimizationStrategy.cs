using Flowthru.Extensions.ML.UMAP.Core;
using MathNet.Numerics.LinearAlgebra;

namespace Flowthru.Extensions.ML.UMAP.Strategies.LayoutOptimization;

/// <summary>
/// Strategy interface for optimizing low-dimensional embeddings via stochastic gradient descent.
/// This is the seventh phase of the UMAP algorithm.
/// </summary>
/// <remarks>
/// <para>
/// The layout optimization phase refines the initial embedding by minimizing the fuzzy set
/// cross entropy between the high-dimensional and low-dimensional fuzzy simplicial sets.
/// This is done through stochastic gradient descent with two types of forces:
/// </para>
/// <list type="bullet">
///   <item><description><b>Attractive forces</b>: Pull connected points closer based on graph edge weights</description></item>
///   <item><description><b>Repulsive forces</b>: Push non-connected points apart via negative sampling</description></item>
/// </list>
/// <para>
/// The force curves are parameterized by <c>a</c> and <c>b</c>, which are derived from
/// the <c>min_dist</c> and <c>spread</c> hyperparameters via curve fitting.
/// </para>
/// <para>
/// Python UMAP reference: <c>optimize_layout_euclidean()</c> in <c>layouts.py</c> (lines 238-441)
/// </para>
/// </remarks>
public interface ILayoutOptimizationStrategy
{
  /// <summary>
  /// Optimizes the embedding layout using stochastic gradient descent.
  /// </summary>
  /// <param name="initialEmbedding">
  /// Initial embedding from layout initialization strategy.
  /// Shape: (n_samples, n_components)
  /// This matrix will be modified in-place during optimization.
  /// </param>
  /// <param name="graphEdges">
  /// Edges in the fuzzy simplicial set to optimize.
  /// Contains (head_index, tail_index, weight) tuples.
  /// </param>
  /// <param name="samplingSchedule">
  /// Sampling schedule that determines how often each edge is sampled.
  /// Array length matches number of edges.
  /// </param>
  /// <param name="nEpochs">
  /// Number of optimization epochs to run.
  /// Must match the value used to compute the sampling schedule.
  /// </param>
  /// <param name="parameters">
  /// Optimization parameters including learning rate, repulsion strength, etc.
  /// </param>
  /// <param name="random">
  /// Random number generator for negative sampling and reproducibility.
  /// </param>
  /// <returns>
  /// The optimized embedding (same matrix as initialEmbedding, modified in-place).
  /// </returns>
  /// <remarks>
  /// <para>
  /// <b>Implementation requirements:</b>
  /// </para>
  /// <list type="number">
  ///   <item><description>Initialize epoch-tracking arrays for sampling schedule</description></item>
  ///   <item><description>For each epoch:</description>
  ///     <list type="bullet">
  ///       <item><description>Sample edges based on their schedule</description></item>
  ///       <item><description>Apply attractive gradient for sampled edges</description></item>
  ///       <item><description>Apply repulsive gradient via negative sampling</description></item>
  ///       <item><description>Decay learning rate linearly</description></item>
  ///     </list>
  ///   </item>
  ///   <item><description>Report progress if verbosity enabled</description></item>
  /// </list>
  /// </remarks>
  LayoutOptimizationResult Optimize(
    Matrix<float> initialEmbedding,
    GraphEdge[] graphEdges,
    float[] samplingSchedule,
    int nEpochs,
    OptimizationParameters parameters,
    Random random
  );
}

/// <summary>
/// Represents an edge in the fuzzy simplicial set graph.
/// </summary>
/// <param name="Head">Index of the head vertex (source).</param>
/// <param name="Tail">Index of the tail vertex (target).</param>
/// <param name="Weight">Membership strength of this edge.</param>
public readonly record struct GraphEdge(int Head, int Tail, float Weight);

/// <summary>
/// Parameters for layout optimization.
/// </summary>
public sealed record OptimizationParameters
{
  /// <summary>
  /// Curve parameter 'a' for attractive force.
  /// </summary>
  public required float A { get; init; }

  /// <summary>
  /// Curve parameter 'b' for attractive force.
  /// </summary>
  public required float B { get; init; }

  /// <summary>
  /// Initial learning rate (decays linearly to 0).
  /// </summary>
  public required float InitialAlpha { get; init; }

  /// <summary>
  /// Weight applied to negative (repulsive) samples.
  /// </summary>
  public required float Gamma { get; init; }

  /// <summary>
  /// Number of negative samples per positive sample.
  /// </summary>
  public required int NegativeSampleRate { get; init; }

  /// <summary>
  /// Verbosity level for progress reporting.
  /// </summary>
  public int Verbosity { get; init; } = 0;

  /// <summary>
  /// Progress reporter for programmatic tracking.
  /// </summary>
  public IProgress<UmapProgress>? ProgressReporter { get; init; }
}

/// <summary>
/// Result of layout optimization.
/// </summary>
/// <param name="OptimizedEmbedding">
/// The final optimized embedding.
/// Shape: (n_samples, n_components)
/// </param>
/// <param name="FinalLoss">
/// Final cross-entropy loss (if computed).
/// Null if loss tracking is disabled.
/// </param>
public sealed record LayoutOptimizationResult(Matrix<float> OptimizedEmbedding, float? FinalLoss)
{
  /// <summary>
  /// Actual number of epochs completed before termination.
  /// May be less than requested epochs if early stopping was triggered.
  /// </summary>
  public int? ActualEpochs { get; init; }

  /// <summary>
  /// Number of epochs saved by early stopping.
  /// Zero if optimization ran to completion or early stopping was disabled.
  /// </summary>
  public int? EarlyStoppingSaved { get; init; }
}
