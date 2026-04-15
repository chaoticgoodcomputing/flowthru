using System.Collections.Concurrent;
using Flowthru.Misc.ML.UMAP.Strategies.MembershipStrength;
using MathNet.Numerics.LinearAlgebra.Single;

namespace Flowthru.Misc.ML.UMAP.Strategies.MembershipStrength.Implementations;

/// <summary>
/// Exponential kernel-based membership strength computation.
/// Uses the standard UMAP exponential kernel to convert distances into probabilities.
/// </summary>
/// <remarks>
/// <para>
/// This is the standard UMAP approach for computing fuzzy set membership strengths.
/// It applies an exponential kernel centered at the local connectivity distance (ρᵢ)
/// with bandwidth σᵢ:
/// </para>
/// <code>
/// μᵢⱼ = {
///   1.0                           if dᵢⱼ ≤ ρᵢ or σᵢ = 0
///   exp(-(dᵢⱼ - ρᵢ) / σᵢ)        otherwise
/// }
/// </code>
/// <para>
/// After computing directed strengths, the algorithm applies fuzzy set operations to
/// symmetrize the graph. The set operation interpolates between fuzzy union and intersection:
/// </para>
/// <code>
/// μ = α(μ_forward + μ_reverse - μ_forward × μ_reverse) + (1-α)(μ_forward × μ_reverse)
/// </code>
/// <para>
/// where α is the set operation mix ratio (typically 1.0 for pure fuzzy union).
/// </para>
/// <para>
/// <b>Characteristics:</b>
/// </para>
/// <list type="bullet">
///   <item><description><b>Time complexity</b>: O(n × k) for computing strengths</description></item>
///   <item><description><b>Space complexity</b>: O(n × k) sparse matrix</description></item>
///   <item><description><b>Graph density</b>: Approximately k edges per node</description></item>
///   <item><description><b>Thread-safe</b>: Yes for reading, exclusive write access needed</description></item>
/// </list>
/// <para>
/// Python reference: <c>compute_membership_strengths()</c> in <c>umap_.py</c> (lines ~260-330)
/// and fuzzy set operations in <c>fuzzy_simplicial_set()</c> (lines ~450-470).
/// </para>
/// </remarks>
public sealed class ExponentialKernel : IMembershipStrengthStrategy
{
  /// <inheritdoc />
  public SparseMatrix ComputeMembershipStrengths(
    int[][] knnIndices,
    float[][] knnDistances,
    float[] sigmas,
    float[] rhos,
    float setOpMixRatio = 1.0f
  )
  {
    int nSamples = knnIndices.Length;

    // Build sparse matrix in COO format (coordinate list) using parallel batching
    var entriesBag = new ConcurrentBag<List<(int row, int col, float value)>>();

    // Compute directed membership strengths in parallel
    Parallel.For(
      0,
      nSamples,
      () => new List<(int row, int col, float value)>(),
      (i, loopState, localList) =>
      {
        for (int j = 0; j < knnIndices[i].Length; j++)
        {
          int neighbor = knnIndices[i][j];
          if (neighbor == -1)
          {
            continue; // Disconnected vertex
          }

          float distance = knnDistances[i][j];
          float val;

          // Skip self-loops (each point to itself)
          if (neighbor == i)
          {
            val = 0.0f;
          }
          // Apply exponential kernel
          else if (distance - rhos[i] <= 0.0f || sigmas[i] == 0.0f)
          {
            val = 1.0f; // Within local connectivity radius
          }
          else
          {
            val = MathF.Exp(-((distance - rhos[i]) / sigmas[i]));
          }

          // Only add non-zero entries
          if (val > 1e-10f)
          {
            localList.Add((i, neighbor, val));
          }
        }
        return localList;
      },
      localList => entriesBag.Add(localList)
    );

    // Flatten all thread-local lists into a single collection
    var entries = entriesBag.SelectMany(list => list);

    // Create sparse matrix from COO format
    var graph = SparseMatrix.OfIndexed(nSamples, nSamples, entries);

    // Apply fuzzy set operations to symmetrize
    // Python reference: lines ~460-470 in fuzzy_simplicial_set()
    var transpose = (SparseMatrix)graph.Transpose();
    var prodMatrix = graph.PointwiseMultiply(transpose);

    // Fuzzy union/intersection interpolation:
    // result = α(A + B - A∘B) + (1-α)(A∘B)
    //        = α·A + α·B - α·(A∘B) + (1-α)·(A∘B)
    //        = α(A + B) + (1-2α)(A∘B)
    var combined = graph
      .Add(transpose)
      .Multiply(setOpMixRatio)
      .Add(prodMatrix.Multiply(1.0f - 2.0f * setOpMixRatio));

    // Return as sparse matrix - Math.NET's sparse operations already handle zeros efficiently
    return SparseMatrix.OfMatrix(combined);
  }
}
