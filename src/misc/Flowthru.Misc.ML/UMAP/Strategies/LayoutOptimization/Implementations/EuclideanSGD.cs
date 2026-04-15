using MathNet.Numerics.LinearAlgebra;

namespace Flowthru.Misc.ML.UMAP.Strategies.LayoutOptimization.Implementations;

/// <summary>
/// Standard Euclidean distance SGD optimizer for UMAP layout optimization.
/// </summary>
/// <remarks>
/// <para>
/// <b>⚠️ NOTE:</b> This is the reference implementation retained for testing and historical purposes.
/// For production use, prefer <see cref="EuclideanSGDOptimized"/> which provides 1.5-2x speedup
/// through direct array access and early stopping while maintaining identical embedding quality.
/// </para>
/// <para>
/// This implementation follows the Python UMAP reference for Euclidean output spaces.
/// It uses stochastic gradient descent with:
/// </para>
/// <list type="bullet">
///   <item><description>Attractive forces based on graph edge weights and a/b curve parameters</description></item>
///   <item><description>Repulsive forces from negative sampling of non-neighbors</description></item>
///   <item><description>Linear learning rate decay</description></item>
/// </list>
/// <para>
/// <b>Time complexity:</b> O(E × n_epochs + N × k × n_epochs) where E = edges, N = vertices, k = negative samples
/// </para>
/// <para>
/// Python UMAP reference: <c>optimize_layout_euclidean()</c> in <c>layouts.py</c> (lines 238-441)
/// </para>
/// </remarks>
public sealed class EuclideanSGD : ILayoutOptimizationStrategy
{
  private const float MinGradientClip = -4.0f;
  private const float MaxGradientClip = 4.0f;

  /// <summary>
  /// Initializes the optimizer. No state is stored in this implementation, so this is a no-op. In more complex strategies, this could be used to set up data structures or precompute values based on the initial embedding and graph.
  /// </summary>
  /// <param name="initialEmbedding"></param>
  /// <param name="graphEdges"></param>
  /// <param name="samplingSchedule"></param>
  /// <param name="nEpochs"></param>
  /// <param name="parameters"></param>
  /// <param name="random"></param>
  /// <returns></returns>
  public LayoutOptimizationResult Optimize(
    Matrix<float> initialEmbedding,
    GraphEdge[] graphEdges,
    float[] samplingSchedule,
    int nEpochs,
    OptimizationParameters parameters,
    Random random
  )
  {
    ValidateInputs(initialEmbedding, graphEdges, samplingSchedule, nEpochs, parameters);

    var nVertices = initialEmbedding.RowCount;
    var nComponents = initialEmbedding.ColumnCount;

    // Initialize sampling state
    var epochOfNextSample = InitializeEpochTracking(samplingSchedule);
    var epochOfNextNegativeSample = InitializeNegativeSampleTracking(
      samplingSchedule,
      parameters.NegativeSampleRate
    );

    // Run SGD epochs
    for (var epoch = 0; epoch < nEpochs; epoch++)
    {
      var alpha = ComputeLearningRate(epoch, nEpochs, parameters.InitialAlpha);

      OptimizeEpoch(
        initialEmbedding,
        graphEdges,
        samplingSchedule,
        epochOfNextSample,
        epochOfNextNegativeSample,
        epoch,
        alpha,
        nVertices,
        parameters,
        random
      );

      ReportProgress(epoch, nEpochs, parameters);
    }

    return new LayoutOptimizationResult(initialEmbedding, FinalLoss: null);
  }

  /// <summary>
  /// Validates inputs are consistent and in acceptable ranges.
  /// </summary>
  private static void ValidateInputs(
    Matrix<float> embedding,
    GraphEdge[] edges,
    float[] schedule,
    int nEpochs,
    OptimizationParameters parameters
  )
  {
    if (embedding == null || embedding.RowCount == 0)
    {
      throw new ArgumentException("Embedding cannot be null or empty", nameof(embedding));
    }

    if (edges == null || edges.Length == 0)
    {
      throw new ArgumentException("Graph edges cannot be null or empty", nameof(edges));
    }

    if (schedule == null || schedule.Length != edges.Length)
    {
      throw new ArgumentException(
        $"Sampling schedule length ({schedule?.Length ?? 0}) must match edges length ({edges.Length})",
        nameof(schedule)
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
  /// Initializes epoch tracking array for when each edge should next be sampled.
  /// </summary>
  private static float[] InitializeEpochTracking(float[] samplingSchedule)
  {
    var tracking = new float[samplingSchedule.Length];
    Array.Copy(samplingSchedule, tracking, samplingSchedule.Length);
    return tracking;
  }

  /// <summary>
  /// Initializes epoch tracking for negative samples (sampled at a different rate).
  /// </summary>
  private static float[] InitializeNegativeSampleTracking(
    float[] samplingSchedule,
    int negativeSampleRate
  )
  {
    var tracking = new float[samplingSchedule.Length];
    for (var i = 0; i < samplingSchedule.Length; i++)
    {
      tracking[i] = samplingSchedule[i] / negativeSampleRate;
    }
    return tracking;
  }

  /// <summary>
  /// Computes learning rate with linear decay.
  /// </summary>
  private static float ComputeLearningRate(int currentEpoch, int totalEpochs, float initialAlpha)
  {
    return initialAlpha * (1.0f - (float)currentEpoch / totalEpochs);
  }

  /// <summary>
  /// Optimizes embedding for a single epoch.
  /// </summary>
  private static void OptimizeEpoch(
    Matrix<float> embedding,
    GraphEdge[] edges,
    float[] samplingSchedule,
    float[] epochOfNextSample,
    float[] epochOfNextNegativeSample,
    int currentEpoch,
    float alpha,
    int nVertices,
    OptimizationParameters parameters,
    Random random
  )
  {
    // Process each edge
    for (var i = 0; i < edges.Length; i++)
    {
      // Skip edges that shouldn't be sampled this epoch
      if (epochOfNextSample[i] > currentEpoch)
      {
        continue;
      }

      var edge = edges[i];

      // Apply attractive force for this edge
      ApplyAttractiveForce(embedding, edge.Head, edge.Tail, alpha, parameters.A, parameters.B);

      // Update next sample epoch for this edge
      epochOfNextSample[i] += samplingSchedule[i];

      // Apply repulsive forces via negative sampling
      var nNegativeSamples = (int)(
        (currentEpoch - epochOfNextNegativeSample[i])
        / (samplingSchedule[i] / parameters.NegativeSampleRate)
      );

      for (var p = 0; p < nNegativeSamples; p++)
      {
        var negativeVertex = random.Next(nVertices);

        // Avoid sampling the same vertex
        if (negativeVertex == edge.Head)
        {
          continue;
        }

        ApplyRepulsiveForce(
          embedding,
          edge.Head,
          negativeVertex,
          alpha,
          parameters.Gamma,
          parameters.A,
          parameters.B
        );
      }

      epochOfNextNegativeSample[i] +=
        nNegativeSamples * samplingSchedule[i] / parameters.NegativeSampleRate;
    }
  }

  /// <summary>
  /// Applies attractive force to pull two connected vertices closer.
  /// </summary>
  private static void ApplyAttractiveForce(
    Matrix<float> embedding,
    int head,
    int tail,
    float alpha,
    float a,
    float b
  )
  {
    // Compute current distance
    var distSquared = 0.0f;
    for (var d = 0; d < embedding.ColumnCount; d++)
    {
      var diff = embedding[head, d] - embedding[tail, d];
      distSquared += diff * diff;
    }

    // Avoid division by zero
    if (distSquared <= 0)
    {
      return;
    }

    // Compute gradient magnitude: -2 * a * b * dist^(2b-2) / (1 + a * dist^(2b))
    var gradCoeff = ComputeAttractiveGradient(distSquared, a, b);

    // Apply gradient to both vertices
    for (var d = 0; d < embedding.ColumnCount; d++)
    {
      var diff = embedding[head, d] - embedding[tail, d];
      var grad = ClipGradient(gradCoeff * diff);

      embedding[head, d] += alpha * grad;
      embedding[tail, d] -= alpha * grad;
    }
  }

  /// <summary>
  /// Computes attractive gradient coefficient.
  /// </summary>
  private static float ComputeAttractiveGradient(float distSquared, float a, float b)
  {
    // Gradient: -2ab * dist^(2b-2) / (1 + a*dist^(2b))
    // For dist^2 = d, this becomes: -2ab * d^(b-1) / (1 + a*d^b)

    var distPow2b = MathF.Pow(distSquared, b);
    var distPow2bMinus2 = MathF.Pow(distSquared, b - 1);

    return -2.0f * a * b * distPow2bMinus2 / (1.0f + a * distPow2b);
  }

  /// <summary>
  /// Applies repulsive force to push two non-connected vertices apart.
  /// </summary>
  private static void ApplyRepulsiveForce(
    Matrix<float> embedding,
    int head,
    int negativeVertex,
    float alpha,
    float gamma,
    float a,
    float b
  )
  {
    // Compute current distance
    var distSquared = 0.0f;
    for (var d = 0; d < embedding.ColumnCount; d++)
    {
      var diff = embedding[head, d] - embedding[negativeVertex, d];
      distSquared += diff * diff;
    }

    // Avoid numerical issues
    if (distSquared <= 0)
    {
      return;
    }

    // Compute gradient magnitude: 2 * gamma * b / ((0.001 + dist^2) * (a * dist^(2b) + 1))
    var gradCoeff = ComputeRepulsiveGradient(distSquared, gamma, a, b);

    // Apply gradient only to head vertex (negative sampling)
    // Python UMAP checks grad_coeff > 0.0 before applying (layouts.py line 172)
    if (gradCoeff > 0.0f)
    {
      for (var d = 0; d < embedding.ColumnCount; d++)
      {
        var diff = embedding[head, d] - embedding[negativeVertex, d];
        var grad = ClipGradient(gradCoeff * diff);

        embedding[head, d] += alpha * grad;
      }
    }
  }

  /// <summary>
  /// Computes repulsive gradient coefficient.
  /// </summary>
  private static float ComputeRepulsiveGradient(float distSquared, float gamma, float a, float b)
  {
    // Python UMAP: grad_coeff = 2.0 * gamma * b / ((0.001 + dist_squared) * (a * pow(dist_squared, b) + 1))
    const float epsilon = 0.001f;
    var distPow2b = MathF.Pow(distSquared, b);
    return 2.0f * gamma * b / ((epsilon + distSquared) * (a * distPow2b + 1.0f));
  }

  /// <summary>
  /// Clips gradient to prevent extreme updates.
  /// </summary>
  private static float ClipGradient(float grad)
  {
    if (grad > MaxGradientClip)
    {
      return MaxGradientClip;
    }

    if (grad < MinGradientClip)
    {
      return MinGradientClip;
    }

    return grad;
  }

  /// <summary>
  /// Reports optimization progress.
  /// </summary>
  private static void ReportProgress(
    int currentEpoch,
    int totalEpochs,
    OptimizationParameters parameters
  )
  {
    var progress = (float)currentEpoch / totalEpochs;

    if (parameters.Verbosity >= 1 && currentEpoch % Math.Max(1, totalEpochs / 10) == 0)
    {
      Console.WriteLine(
        $"[Layout Optimization] Epoch {currentEpoch}/{totalEpochs} ({progress * 100:F1}%)"
      );
    }

    parameters.ProgressReporter?.Report(
      new Core.UmapProgress
      {
        Stage = "Layout Optimization",
        Progress = progress,
        Details = $"Epoch {currentEpoch}/{totalEpochs}",
      }
    );
  }
}
