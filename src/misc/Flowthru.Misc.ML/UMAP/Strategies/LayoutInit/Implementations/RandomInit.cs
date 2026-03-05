using Flowthru.Misc.ML.UMAP.Core.Markers;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Single;

namespace Flowthru.Misc.ML.UMAP.Strategies.LayoutInit.Implementations;

/// <summary>
/// Random uniform initialization for fast prototyping and debugging.
/// </summary>
/// <remarks>
/// <para>
/// This strategy initializes embedding coordinates uniformly at random in the range [-10, 10].
/// While this provides the fastest initialization, it typically requires more optimization
/// epochs to converge compared to spectral or PCA initialization.
/// </para>
/// <para>
/// <b>Use cases:</b>
/// </para>
/// <list type="bullet">
///   <item><description>Quick prototyping and experimentation</description></item>
///   <item><description>Debugging optimization algorithms</description></item>
///   <item><description>When data/graph are unavailable for smarter initialization</description></item>
///   <item><description>Fallback when spectral initialization fails (disconnected graph)</description></item>
/// </list>
/// <para>
/// <b>Time complexity:</b> O(n × k) where n = n_samples, k = n_components
/// </para>
/// <para>
/// <b>Space complexity:</b> O(n × k)
/// </para>
/// <para>
/// Python UMAP reference: Lines 1078-1081 in <c>simplicial_set_embedding()</c>
/// </para>
/// </remarks>
public sealed class RandomInit : ILayoutInitStrategy
{
  private const float MinCoord = -10.0f;
  private const float MaxCoord = 10.0f;

  /// <summary>
  /// Initializes embedding with uniform random coordinates.
  /// </summary>
  /// <param name="data">Original data (unused for random initialization).</param>
  /// <param name="graph">Graph (unused for random initialization).</param>
  /// <param name="nComponents">Target embedding dimensionality.</param>
  /// <param name="random">Random number generator for coordinate sampling.</param>
  /// <returns>Random embedding normalized to [-10, 10] range.</returns>
  public LayoutInitResult InitializeLayout(
    Matrix<float>? data,
    SparseMatrix graph,
    int nComponents,
    Random random
  )
  {
    ValidateInputs(graph, nComponents);

    var nSamples = graph.RowCount;
    var embedding = GenerateRandomEmbedding(nSamples, nComponents, random);

    return new LayoutInitResult(embedding, "random");
  }

  /// <summary>
  /// Validates that inputs are in acceptable ranges.
  /// </summary>
  private static void ValidateInputs(SparseMatrix graph, int nComponents)
  {
    if (nComponents < 1)
    {
      throw new ArgumentException(
        $"Number of components must be at least 1, got {nComponents}",
        nameof(nComponents)
      );
    }

    if (nComponents >= graph.RowCount)
    {
      throw new ArgumentException(
        $"Number of components ({nComponents}) must be less than number of samples ({graph.RowCount})",
        nameof(nComponents)
      );
    }
  }

  /// <summary>
  /// Generates random embedding coordinates uniformly distributed in [-10, 10].
  /// </summary>
  private static Matrix<float> GenerateRandomEmbedding(int nSamples, int nComponents, Random random)
  {
    var embedding = DenseMatrix.Create(nSamples, nComponents, 0.0f);

    for (var i = 0; i < nSamples; i++)
    {
      for (var j = 0; j < nComponents; j++)
      {
        embedding[i, j] = SampleUniform(random, MinCoord, MaxCoord);
      }
    }

    return embedding;
  }

  /// <summary>
  /// Samples a uniform random float in the specified range.
  /// </summary>
  private static float SampleUniform(Random random, float min, float max)
  {
    return min + (float)random.NextDouble() * (max - min);
  }
}
