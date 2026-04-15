using MathNet.Numerics.LinearAlgebra.Single;

namespace Flowthru.Misc.ML.UMAP.Strategies.MembershipStrength;

/// <summary>
/// Strategy interface for computing fuzzy simplicial set membership strengths.
/// This is the third phase of the UMAP algorithm.
/// </summary>
/// <remarks>
/// <para>
/// The membership strength phase converts k-NN distances into membership probabilities
/// for the fuzzy simplicial set. Each edge (i,j) gets a membership strength μᵢⱼ ∈ [0,1]
/// that represents how strongly point j belongs to the fuzzy neighborhood of point i.
/// </para>
/// <para>
/// <b>Standard approach (exponential kernel):</b>
/// </para>
/// <code>
/// μᵢⱼ = exp(-(max(0, dᵢⱼ - ρᵢ)) / σᵢ)
/// </code>
/// <para>
/// where dᵢⱼ is the distance, ρᵢ is the local connectivity distance, and σᵢ is the bandwidth.
/// </para>
/// <para>
/// After computing directed membership strengths, fuzzy set operations (union/intersection)
/// combine them into a symmetric global graph structure.
/// </para>
/// <para>
/// Python UMAP reference: <c>compute_membership_strengths()</c> and <c>fuzzy_simplicial_set()</c>
/// functions in <c>umap_.py</c> (lines ~260-450)
/// </para>
/// </remarks>
public interface IMembershipStrengthStrategy
{
  /// <summary>
  /// Computes membership strengths for the fuzzy simplicial set.
  /// </summary>
  /// <param name="knnIndices">
  /// Indices of k-nearest neighbors for each point.
  /// Array shape: (n_samples, n_neighbors)
  /// </param>
  /// <param name="knnDistances">
  /// Distances to k-nearest neighbors for each point.
  /// Array shape: (n_samples, n_neighbors)
  /// </param>
  /// <param name="sigmas">
  /// Bandwidth parameters from local metric computation.
  /// Array shape: (n_samples,)
  /// </param>
  /// <param name="rhos">
  /// Local connectivity distances from local metric computation.
  /// Array shape: (n_samples,)
  /// </param>
  /// <param name="setOpMixRatio">
  /// Interpolation between fuzzy union (1.0) and intersection (0.0).
  /// Controls how local fuzzy sets are combined into global structure.
  /// Range: [0.0, 1.0]
  /// </param>
  /// <returns>
  /// A sparse matrix representing the fuzzy simplicial set.
  /// Shape: (n_samples, n_samples)
  /// Matrix[i,j] represents the membership strength of the edge from i to j.
  /// After set operations, the matrix should be symmetric.
  /// </returns>
  /// <remarks>
  /// <para>
  /// <b>Implementation requirements:</b>
  /// </para>
  /// <list type="number">
  ///   <item><description>Compute directed membership strengths μᵢⱼ for each edge</description></item>
  ///   <item><description>Apply fuzzy set operation: μ = α(μᵢⱼ + μⱼᵢ - μᵢⱼμⱼᵢ) + (1-α)μᵢⱼμⱼᵢ</description></item>
  ///   <item><description>Eliminate zero entries from sparse matrix</description></item>
  ///   <item><description>Ensure matrix is symmetric after set operations</description></item>
  /// </list>
  /// </remarks>
  SparseMatrix ComputeMembershipStrengths(
    int[][] knnIndices,
    float[][] knnDistances,
    float[] sigmas,
    float[] rhos,
    float setOpMixRatio = 1.0f
  );
}
