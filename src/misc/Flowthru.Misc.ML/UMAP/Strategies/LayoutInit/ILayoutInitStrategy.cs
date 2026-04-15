using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Single;

namespace Flowthru.Misc.ML.UMAP.Strategies.LayoutInit;

/// <summary>
/// Strategy interface for initializing the low-dimensional embedding before optimization.
/// This is the fifth phase of the UMAP algorithm.
/// </summary>
/// <remarks>
/// <para>
/// The layout initialization phase creates an initial low-dimensional embedding that serves
/// as the starting point for stochastic gradient descent optimization. The quality of this
/// initialization significantly impacts:
/// </para>
/// <list type="bullet">
///   <item><description><b>Convergence speed</b>: Better initializations require fewer optimization epochs</description></item>
///   <item><description><b>Final quality</b>: Good initializations help avoid poor local minima</description></item>
///   <item><description><b>Reproducibility</b>: Deterministic initializations enable consistent results</description></item>
/// </list>
/// <para>
/// <b>Common initialization strategies:</b>
/// </para>
/// <list type="bullet">
///   <item><description><b>Spectral</b>: Eigendecomposition of graph Laplacian (high quality, O(n²) time, recommended for datasets &lt; 10k samples)</description></item>
///   <item><description><b>PCA</b>: Principal component analysis of original data (medium quality, O(n×d) time)</description></item>
///   <item><description><b>Random</b>: Uniform random positions (low quality, O(n) time, fastest option)</description></item>
///   <item><description><b>Precomputed</b>: User-provided initialization (quality varies)</description></item>
/// </list>
/// <para>
/// All initializations are normalized to the range [-10, 10] with small random noise
/// to prevent degenerate configurations and improve numerical stability.
/// </para>
/// <para>
/// Python UMAP reference: Lines 1078-1148 in <c>simplicial_set_embedding()</c> function
/// </para>
/// </remarks>
public interface ILayoutInitStrategy
{
  /// <summary>
  /// Initializes the low-dimensional embedding layout.
  /// </summary>
  /// <param name="data">
  /// Original high-dimensional data matrix.
  /// Shape: (n_samples, n_features)
  /// May be null for precomputed distance-based initialization.
  /// </param>
  /// <param name="graph">
  /// Refined fuzzy simplicial set graph after pruning.
  /// Shape: (n_samples, n_samples)
  /// Used by spectral and graph-based initialization methods.
  /// </param>
  /// <param name="nComponents">
  /// Target dimensionality of the embedding.
  /// Typically 2 or 3 for visualization, or higher for downstream tasks.
  /// Must be at least 1 and less than n_samples.
  /// </param>
  /// <param name="random">
  /// Random number generator for reproducible randomization.
  /// Used for noise injection and random initialization.
  /// </param>
  /// <returns>
  /// Initial embedding matrix with coordinates normalized to [-10, 10] range.
  /// Shape: (n_samples, n_components)
  /// </returns>
  /// <remarks>
  /// <para>
  /// <b>Implementation requirements:</b>
  /// </para>
  /// <list type="number">
  ///   <item><description>Generate or compute initial coordinates</description></item>
  ///   <item><description>Add small random noise to avoid degeneracies</description></item>
  ///   <item><description>Normalize to [-10, 10] range for numerical stability</description></item>
  ///   <item><description>Ensure output is C-contiguous (row-major) for optimization</description></item>
  ///   <item><description>Handle disconnected graph components gracefully</description></item>
  /// </list>
  /// <para>
  /// <b>Performance considerations:</b>
  /// </para>
  /// <list type="bullet">
  ///   <item><description>Spectral methods require eigenvalue decomposition: O(n²) to O(n³)</description></item>
  ///   <item><description>PCA methods require SVD: O(min(n,d) × n × d)</description></item>
  ///   <item><description>Random methods are O(n × k) where k is n_components</description></item>
  /// </list>
  /// </remarks>
  LayoutInitResult InitializeLayout(
    Matrix<float>? data,
    SparseMatrix graph,
    int nComponents,
    Random random
  );
}

/// <summary>
/// Result of layout initialization.
/// </summary>
/// <param name="Embedding">
/// Initial low-dimensional embedding coordinates.
/// Shape: (n_samples, n_components)
/// Values are normalized to approximately [-10, 10] range.
/// </param>
/// <param name="InitializationMethod">
/// Human-readable description of the initialization method used.
/// Useful for logging and debugging.
/// </param>
public sealed record LayoutInitResult(Matrix<float> Embedding, string InitializationMethod);
